using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace m.format.conv
{
    /// <summary>
    /// Converts Markdown to HTML.
    /// </summary>
    /// <version>2.2.0</version>
    /// <date>2025-08-04</date>
    /// <author>Miloš Perunović</author>
    public class ConvMarkdownHtml
    {
        #region Main methods for converting Markdown to HTML

        /// <summary>
        /// Converts Markdown document to HTML.
        /// </summary>
        /// <param name="md">The Markdown document as a string.</param>
        /// <param name="lang">Language code (e.g. "en", "cnr")</param>
        /// <param name="head">Additional head elements (e.g. CSS links)</param>
        /// <returns>HTML representation of the markdown document</returns>
        public static string Convert(string md, string lang = "en", string head = null)
        {
            Stopwatch sw = Stopwatch.StartNew();

            // Convert Markdown to HTML body
            // This method will also extract metadata from the document, such as title, author, and date.
            string body = new ConvMarkdownHtml().ToHtmlBody(md, out Dictionary<string, string> metadata);

            // Generate html meta tags from metadata
            StringBuilder meta = new StringBuilder();
            if (!metadata.ContainsKey("title")) { metadata["title"] = "Untitled Document"; }
            foreach (KeyValuePair<string, string> pair in metadata)
            {
                string key = pair.Key.ToLowerInvariant();

                if (key == "title")
                {
                    meta.Append($"  <title>{EscapeHtmlQ(pair.Value)}</title>\n");
                }
                else
                {
                    meta.Append($"  <meta name=\"{EscapeHtmlQ(pair.Key)}\" content=\"{EscapeHtmlQ(pair.Value)}\">\n");
                }
            }

            sw.Stop();
            double seconds = (double)sw.ElapsedTicks / Stopwatch.Frequency;
            Console.WriteLine($"Markdown -> HTML conv.: {seconds} sec.");

            // Generate the HTML document
            return
                "<!DOCTYPE html>\n" +
                $"<html lang=\"{lang}\">\n" +
                "<head>\n" +
                "  <meta charset=\"utf-8\">" +
                (meta.Length > 0 ? $"\n{meta}" : "") +
                (head ?? "") +
                "</head>\n" +
                "<body>\n" +
                $"{body}\n" +
                "</body>\n" +
                "</html>\n";
        }

        /// <summary>
        /// States used during Markdown parsing.
        /// </summary>
        private enum State
        {
            Empty,
            Heading,
            Paragraph,
            TaskList,
            UnorderedList,
            OrderedList,
            Blockquote,
            CodeBlock,
            Table,
            RawHtmlCode,
            HorizontalRule,
            Break
        }

        /// <summary>
        /// StringBuilder for accumulating the HTML output.
        /// This is used to build the final HTML string efficiently.
        /// </summary>
        private StringBuilder Out;

        /// <summary>
        /// Lines of markdown text.
        /// </summary>
        private string[] Lines;

        /// <summary>
        /// Number of lines in the markdown text.
        /// </summary>
        private int LinesCount;

        /// <summary>
        /// Current line number.
        /// </summary>
        private int LineNum;

        /// <summary>
        /// Paragraph content.
        /// This is used to accumulate text for the current paragraph being processed.
        /// </summary>
        private readonly StringBuilder Para = new StringBuilder();

        /// <summary>
        /// Current state of Markdown parsing.
        /// This is used to track the current context while parsing the Markdown text.
        /// </summary>
        private State CurrState;

        /// <summary>
        /// Previous state of Markdown parsing.
        /// This is used to remember the last state before the current one, allowing for proper context handling
        /// </summary>
        private State PrevState = State.Empty;

        // List states
        private State ListState = State.Empty;

        /// <summary>
        /// Unordered, ordered, or task list last level.
        /// This is used to track the current level of nesting in lists.
        /// </summary>
        private int ListLastLevel;

        /// <summary>
        /// LIFO stack for closing tags in lists.
        /// This stack is used to keep track of the closing tags for lists (ordered and unordered lists).
        /// </summary>
        private readonly Stack<string> ListClosingTags = new Stack<string>();

        /// <summary>
        /// Converts Markdown document to HTML.
        /// </summary>
        /// <param name="doc">Markdown document</param>
        /// <param name="metadata">Document metadata. This includes title, author, date,...</param>
        /// <returns>HTML representation of the markdown</returns>
        private string ToHtmlBody(string doc, out Dictionary<string, string> metadata)
        {
            Out = new StringBuilder(doc.Length * 2); // Pre-allocate space for the HTML output

            Lines = doc.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            LinesCount = Lines.Length;
            metadata = new Dictionary<string, string>();

            int startLine = 0;
            int emptyCnt = 0;

            // Detect YAML Front Matter
            string trimLine = Lines[0].Trim();
            if (LinesCount > 0 && trimLine == "---")
            {
                int end = -1;
                int i = 0;
                while (++i < LinesCount && i < 20)
                {
                    trimLine = Lines[i].Trim();
                    if (trimLine == "---" || trimLine == "...")
                    {
                        end = i;
                        break;
                    }
                }
                if (end > 0)
                {
                    for (i = 1; i < end; i++)
                    {
                        string line = Lines[i].Trim();
                        int sep = line.IndexOf(':');
                        if (sep > 0)
                        {
                            string key = line.Substring(0, sep).Trim().ToLowerInvariant();
                            string value = line.Substring(sep + 1).Trim();

                            switch (key)
                            {
                                case "title":
                                    metadata["title"] = value;
                                    break;
                                case "author":
                                    metadata["author"] = value;
                                    break;
                                case "version":
                                    metadata["version"] = value;
                                    break;
                                case "date":
                                    metadata["date"] = value;
                                    break;
                                case "description":
                                    metadata["description"] = value;
                                    break;
                                case "keywords":
                                    metadata["keywords"] = value;
                                    break;
                                case "license":
                                    metadata["license"] = value;
                                    break;
                            }
                        }
                    }
                    startLine = end + 1;
                }
            }

            // Parse footnotes at the end of the document
            ParseFootnotes(Lines, Math.Max(0, LinesCount - FootnoteScanLines));

            for (LineNum = startLine; LineNum < LinesCount; LineNum++)
            {
                string line = Lines[LineNum];
                trimLine = line.Trim();
                char firstChar = trimLine.Length > 0 ? trimLine[0] : '\0'; // First character of the line. If empty, it's '\0'

                // Determine the indentation level
                int indentSpc = 0; // Count of leading spaces
                int indentPos = 0; // Position of the first non-space character
                if (firstChar != '\0')
                {
                    while (indentPos < line.Length && (line[indentPos] == ' ' || line[indentPos] == '\t'))
                    {
                        indentSpc += (line[indentPos] == '\t') ? 4 : 1;
                        indentPos++;
                    }
                }

                // Empty line
                if (firstChar == '\0')
                {
                    CurrState = State.Empty;
                    emptyCnt++;
                    if (PrevState == State.Paragraph)
                    {
                        ProcessParagraph();
                        Out.Append("</p>\n");
                    }
                    else if (PrevState == State.UnorderedList || PrevState == State.TaskList || PrevState == State.OrderedList || PrevState == State.Blockquote)
                    {
                        CurrState = PrevState;
                    }
                    else if (PrevState == State.Empty && emptyCnt == 3)
                    {
                        Out.Append("<br>\n");
                        emptyCnt = 1;
                    }
                }

                // Line break
                else if (trimLine == "\\")
                {
                    CurrState = State.Break;
                    Out.Append("<br>\n");
                    emptyCnt = 1;
                    continue;
                }

                // Heading
                else if (firstChar == '#' && ProcessHeading(line, out int level, out string content, out string id))
                {
                    CurrState = State.Heading;
                    CloseBlock();
                    EnsureEmptyLine();
                    line = $"<h{level} id=\"{id}\">{content}</h{level}>\n";
                }

                // Unordered list, task lists
                else if (IsUnorderedList(line, indentSpc, indentPos, out level, out _, out content))
                {
                    string inputBox = "";
                    if (TryParseTaskList(line, out string content2, out bool isChecked))
                    {
                        CurrState = State.TaskList;
                        inputBox = $"<input type=\"checkbox\" {(isChecked ? "checked" : "")}>";
                        content = content2;
                    }
                    else
                    {
                        CurrState = State.UnorderedList;
                    }
                    ListState = CurrState;

                    CloseBlock();

                    string ind = new string(' ', level * 2);

                    if (level > ListLastLevel)
                    {
                        line = "";
                        for (int j = 0; j < level - ListLastLevel; j++)
                        {
                            line += $"<ul>\n{ind}<li>";
                            ListClosingTags.Push("</ul>\n");
                        }
                        line += inputBox + content;
                    }
                    else if (level < ListLastLevel)
                    {
                        ListLevelDown(level);
                        line = $"{ind}<li>{inputBox}{content}";
                    }
                    else
                    {
                        line = $"</li>\n{ind}<li>{inputBox}{content}";
                    }
                    ListLastLevel = level;
                }

                // Ordered list
                else if (IsOrderedList(line, indentSpc, indentPos, out level, out _, out content))
                {
                    CurrState = State.OrderedList;
                    ListState = State.OrderedList;

                    CloseBlock();

                    string ind = new string(' ', level * 2);

                    if (level > ListLastLevel)
                    {
                        line = "";
                        for (int j = 0; j < level - ListLastLevel; j++)
                        {
                            line += $"<ol>\n{ind}<li>";
                            ListClosingTags.Push("</ol>\n");
                        }
                        line += content;
                    }
                    else if (level < ListLastLevel)
                    {
                        ListLevelDown(level);
                        line = $"</li>\n{ind}<li>{content}";
                    }
                    else
                    {
                        line = $"</li>\n{ind}<li>{content}";
                    }
                    ListLastLevel = level;
                }

                // Horizontal rule
                else if (IsHorizontalRule(trimLine))
                {
                    CurrState = State.HorizontalRule;
                    CloseBlock();
                    EnsureEmptyLine();
                    line = "<hr>\n";
                }

                // Blockquote
                else if (firstChar == '>')
                {
                    CurrState = State.Blockquote;
                    content = ParseInlineStyles(line.Substring(1).TrimStart());
                    if (PrevState != State.Blockquote)
                    {
                        CloseBlock();
                        Out.Append("<blockquote>\n");
                    }
                    line = content.Length == 0 ? "" : $"<p>{content}</p>\n";
                }

                // Code block
                else if ((firstChar == '`' || firstChar == '~') && IsCodeFence(trimLine, out string fence, out string lang))
                {
                    CurrState = State.CodeBlock;
                    if (PrevState != State.CodeBlock)
                    {
                        CloseBlock();
                        Out.Append($"<pre><code class=\"{lang}\">\n");
                        bool endBlock = false;
                        while (!endBlock && ++LineNum < LinesCount)
                        {
                            if (Lines[LineNum].Trim() == fence)
                            {
                                Out.Append("</code></pre>\n");
                                endBlock = true;
                                CurrState = State.Empty;
                            }
                            else
                            {
                                Out.Append($"{EscapeHtml(Lines[LineNum])}\n");
                            }
                        }
                    }
                    line = "";
                }

                // Raw HTML
                else if (firstChar == '<' && ProcessRawHtml(line, trimLine, out State state))
                {
                    CloseBlock();
                    CurrState = state;
                    continue;
                }

                // Tables
                else if (firstChar == '|' && LineNum < LinesCount - 2 && IsTableHeaderSeparator(Lines[LineNum + 1]))
                {
                    CurrState = State.Table;
                    CloseBlock();
                    string table = ParseTable(Lines, LineNum, out int linesConsumed);
                    Out.Append(table);
                    line = "";
                    LineNum = linesConsumed + LineNum - 1;
                }

                // Table of Contents placeholder
                else if (trimLine == "[TOC]")
                {
                    Out.Append("{{TOC_PLACEHOLDER}}");
                    continue;
                }

                // Paragraph
                else
                {
                    CurrState = State.Paragraph;
                    CloseBlock(false);
                    if (line.EndsWith("  ") || line.EndsWith("\\"))
                    {
                        Para.Append(line.TrimEnd(' ', '\\') + "\n");
                    }
                    else
                    {
                        Para.Append(line);
                    }
                    line = "";
                }

                PrevState = CurrState;

                // Adding line to HTML
                if (CurrState != State.Empty)
                {
                    emptyCnt = 0;
                    Out.Append(line);
                }
            }

            CurrState = State.Empty;
            CloseBlock();

            // Add footnote definitions at the end of the document
            if (UsedFootnotes.Count > 0)
            {
                EnsureEmptyLine();
                Out.Append("<div class=\"footnotes\">\n<ul>\n");

                foreach (string id in UsedFootnotes)
                {
                    if (!FootnoteDefinitions.TryGetValue(id, out string text))
                    {
                        continue; // footnote is not defined, skip
                    }
                    // Create footnote with link back to reference
                    Out.Append($"<li id=\"fn{id}\"><sup>{id}</sup> {ParseInlineStyles(text)} <a href=\"#ref{id}\" class=\"footnote-backref\">↩</a></li>\n");
                }

                Out.Append("</ul>\n</div>\n");
            }

            GenerateWarningsReport();

            // Generate Table of Contents (TOC) and insert it into the body
            string toc = GenerateToc(TocHeadings);
            if (toc.Length > 0)
            {
                Out.Replace("{{TOC_PLACEHOLDER}}", toc);
            }

            return Out.ToString();
        }

        /// <summary>
        /// Processes the current paragraph by converting inline styles and appending it to the HTML body.
        /// </summary>
        private void ProcessParagraph()
        {
            if (Para.Length > 0)
            {
                Out.Append(ParseInlineStyles(Para.ToString()));
                Para.Clear();
            }
        }

        /// <summary>
        /// Closes the current block in the HTML document.
        /// </summary>
        private void CloseBlock(bool procPara = true)
        {
            if (procPara) { ProcessParagraph(); }

            if (ListState != State.Empty && !(CurrState == State.UnorderedList || CurrState == State.TaskList || CurrState == State.OrderedList))
            {
                ListLevelDown(0);
            }

            if (PrevState == State.Blockquote)
            {
                Out.Append("</blockquote>\n");
            }
            else if (PrevState == State.CodeBlock)
            {
                Out.Append("</code></pre>\n");
            }

            string line = "";

            if (CurrState == State.Paragraph)
            {
                if (PrevState == State.Paragraph)
                {
                    Para.Append(" ");
                }
                else
                {
                    line = "<p>";
                }
            }
            else if (PrevState == State.Paragraph)
            {
                line = "</p>\n";
            }

            if (line.Length > 0) { Out.Append(line); }
        }

        /// <summary>
        /// Closes list levels in the current document structure down to the specified level.
        /// </summary>
        /// <param name="level">
        /// The target list level to close down to. Must be less than or equal to the current list level.
        /// </param>
        private void ListLevelDown(int level)
        {
            Out.Append("</li>\n");

            for (int j = 0; j < ListLastLevel - level; j++)
            {
                string tag = ListClosingTags.Pop();
                Out.Append(tag);
            }

            if (level == 0)
            {
                ListState = State.Empty;
                ListLastLevel = 0;
            }
        }

        #endregion

        #region Helper methods

        /// <summary>
        /// Escapes HTML special characters in the input string.
        /// This method replaces characters like '&', '<', and '>' with their corresponding HTML entities.
        /// </summary>
        private static string EscapeHtml(string input)
        {
            return input.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        /// <summary>
        /// Escapes HTML special characters in the input string.
        /// This method replaces characters like '&', '<', '>', and '"' with their corresponding HTML entities.
        /// </summary>
        private static string EscapeHtmlQ(string input)
        {
            return input.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        /// <summary>
        /// Adds an empty line to the Markdown output.
        /// This method ensures that there are two newlines before the next content,
        /// which is the standard way to separate paragraphs in Markdown.
        /// </summary>
        private void EnsureEmptyLine()
        {
            int outLen = Out.Length;
            if (outLen < 2 || (Out[outLen - 1] == '\n' && Out[outLen - 2] == '\n'))
            {
                return;
            }
            if (outLen >= 1 && Out[outLen - 1] == '\n')
            {
                Out.Append('\n');
            }
            else
            {
                Out.Append("\n\n");
            }
        }

        #endregion

        #region Inline conversions

        /// <summary>
        /// Counters for the number of open tags.
        /// </summary>
        private int CntBld, CntItl, CntHl, CntDel, CntSub, CntSup;

        /// <summary>
        /// Parses a line of text and converts inline style markers (e.g., bold, italic) into corresponding HTML tags.
        /// </summary>
        /// <returns>
        /// A string where inline style markers (e.g., <c>*</c> for bold and italic) are replaced with their
        /// corresponding HTML tags (e.g., <c>&lt;strong&gt;</c> and <c>&lt;em&gt;</c>).
        /// </returns>
        private string ParseInlineStyles(string line)
        {
            StringBuilder res = new StringBuilder();

            int len = line.Length;

            if (len == 0) { return ""; }

            // Check if the line contains a URL or email address to skip auto-link checks
            bool skipCheckAutolink = true;
            if (len > 5 && (line.Contains("://") || line.Contains("@")))
            {
                skipCheckAutolink = false;
            }

            for (int pos = 0; pos < len; pos++)
            {
                char c = line[pos];

                if (c == '\\')
                {
                    if (pos + 1 < len)
                    {
                        // Escape character
                        pos++;
                        switch (line[pos])
                        {
                            case '<': res.Append("&lt;"); break;
                            case '>': res.Append("&gt;"); break;
                            case '&': res.Append("&amp;"); break;
                            case '"': res.Append("&quot;"); break;
                            default: res.Append(line[pos]); break;
                        }
                        continue;
                    }
                    else
                    {
                        res.Append(c);
                        continue;
                    }
                }

                // Line break
                else if (c == '\n')
                {
                    res.Append("<br>");
                }

                // HTML entity
                else if (c == '&')
                {
                    int semicolonPos = line.IndexOf(';', pos + 1);
                    if (semicolonPos > pos)
                    {
                        string entity = line.Substring(pos, semicolonPos - pos + 1);
                        string decoded = WebUtility.HtmlDecode(entity);
                        if (decoded != entity)
                        {
                            res.Append(entity);
                            pos += entity.Length - 1;
                            continue;
                        }
                    }
                    res.Append("&amp;");
                    continue;
                }

                // Less-than sign
                else if (c == '<')
                {
                    int tagStart = pos;
                    int j = pos + 1;

                    // Go to the end of the potential tag
                    while (j < line.Length && line[j] != '>') { j++; }

                    if (j < line.Length && line[j] == '>')
                    {
                        string tagCandidate = line.Substring(tagStart, j - tagStart + 1);

                        // Check if the tag is allowed
                        if (!IsDangerousTag(tagCandidate) && IsAllowedHtmlTag(tagCandidate))
                        {
                            res.Append(tagCandidate);
                            pos = j;
                            continue;
                        }
                    }
                    res.Append("&lt;");
                    continue;
                }

                // Greater-than sign
                else if (c == '>')
                {
                    res.Append("&gt;");
                    continue;
                }

                // Footnotes and links
                else if (c == '[')
                {
                    // Footnotes
                    if (line[pos] == '[' && pos + 1 < len && line[pos + 1] == '^')
                    {
                        int end = line.IndexOf(']', pos);
                        if (end > -1)
                        {
                            string id = line.Substring(pos + 2, end - pos - 2);

                            if (!FullScanFootnote && !FootnoteDefinitions.ContainsKey(id))
                            {
                                // Re-parse footnotes in case some definitions are not at the end of the document
                                ParseFootnotes(Lines, 0);
                                FullScanFootnote = true;
                            }

                            if (FootnoteDefinitions.ContainsKey(id))
                            {
                                UsedFootnotes.Add(id);
                                string tooltip = "";
                                // Add or get the existing footnote number
                                if (!FootnoteNumbers.TryGetValue(id, out _))
                                {
                                    FootnoteNumbers[id] = NextFootnoteNumber++;
                                    tooltip = FootnoteDefinitions[id].Replace("\"", "&quot;");
                                }
                                // HTML link in the text uses href to "fn" and id "ref"
                                res.Append($"<sup><a href=\"#fn{id}\" id=\"ref{id}\" title=\"{tooltip}\">{id}</a></sup>");
                                pos = end; // skip the parsed part
                                continue;
                            }
                        }
                    }

                    // Links. Example: [link](https://example.com)
                    else if (TryParseLink(line, pos, out string linkText, out string url, out string title, out int end))
                    {
                        res.Append($"<a href=\"{EscapeHtml(url)}{(title == null ? "" : $"\" title=\"{EscapeHtml(title)}")}\">{linkText}</a>");
                        pos = end; // skip the parsed part
                        continue;
                    }
                }

                // Autolinks. Example: https://example.com, <https://example.com>, user@example.com, <user@example.com>
                else if (!skipCheckAutolink && TryParseAutoLink(line, pos, out string linkText2, out string url2, out string title2, out string cls, out int end2))
                {
                    res.Append($"<a href=\"{EscapeHtml(url2)}{(title2 == null ? "" : $"\" title=\"{EscapeHtml(title2)}")}\"{cls}>{linkText2}</a>");
                    pos = end2; // skip the parsed part
                    continue;
                }

                // Image. Example: ![Logo za HTML 5](https://www.w3schools.com/html/html5.gif \"HTML 5 Logo\")
                else if (c == '!' && pos + 1 < len && line[pos + 1] == '[')
                {
                    if (TryParseImage(line, pos, out string altText, out string url, out string title, out int end))
                    {
                        res.Append($"<img src=\"{url}\" alt=\"{altText}\"{(title == null ? "" : $" title=\"{title}\"")}>");
                        pos = end; // skip the parsed part
                        continue;
                    }
                }

                // Inline code
                else if (c == '`')
                {
                    if (TryParseInlineCode(line, pos, out int end, out string codeContent))
                    {
                        // HTML-escape the content
                        string html = $"<code>{WebUtility.HtmlEncode(codeContent)}</code>";
                        res.Append(html);
                        pos = end; // skip the parsed part
                        continue;
                    }
                }

                // Basic styles
                else if (c == '*')
                {
                    char c2 = (pos + 1 < len) ? line[pos + 1] : '\0';
                    char c3 = (pos + 2 < len) ? line[pos + 2] : '\0';

                    // Bold + Italic
                    if (c2 == '*' && c3 == '*')
                    {
                        if (CntBld == 0 && CntItl == 0 && c2 != ' ')
                        {
                            pos += 2;
                            CntBld++; CntItl++; res.Append("<strong><em>");
                        }
                        else
                        {
                            if (CntItl > 0)
                            {
                                pos++;
                                CntItl--;
                                res.Append("</em>");
                            }
                            if (CntBld > 0)
                            {
                                pos++;
                                CntBld--;
                                res.Append("</strong>");
                            }
                        }
                        continue;
                    }

                    // Bold
                    else if (c2 == '*')
                    {
                        pos++;
                        if (CntBld == 0 && c3 != ' ')
                        {
                            CntBld++; res.Append("<strong>");
                        }
                        else if (CntBld > 0)
                        {
                            CntBld--; res.Append("</strong>");
                        }
                        else
                        {
                            ReportWarning($"Incorrectly written character for \"bold\" [pos {pos}]");
                        }
                        continue;
                    }

                    // Italic
                    else
                    {
                        if (CntItl == 0 && c2 != ' ')
                        {
                            CntItl++; res.Append("<em>");
                        }
                        else if (CntItl > 0)
                        {
                            CntItl--; res.Append("</em>");
                        }
                        else
                        {
                            ReportWarning($"Incorrectly written character for \"italic\" [pos {pos}]");
                        }
                        continue;
                    }
                }

                // Highlight
                else if (c == '=' && pos + 1 < len && line[pos + 1] == '=')
                {
                    int j = pos + 2;

                    if (CntHl == 0)
                    {
                        // Check that there are not more than 2 '=' characters
                        if (j < len && line[j] == '=')
                        {
                            // Too many = characters -> not highlighting
                            res.Append("==");
                            pos++;
                            continue;
                        }

                        // Check that there is no space after == (e.g. == text)
                        if (j < len && line[j] == ' ')
                        {
                            res.Append("==");
                            pos++;
                            continue;
                        }

                        CntHl++;
                        res.Append("<mark>");
                    }
                    else
                    {
                        // Closing highlight tag
                        CntHl--;
                        res.Append("</mark>");
                    }

                    pos++; // skip the second '=' character
                    continue;
                }

                // Strikethrough
                else if (c == '~' && pos + 1 < len && line[pos + 1] == '~')
                {
                    int j = pos + 2;

                    if (CntHl == 0)
                    {
                        // Check that there are not more than 2 '~' characters
                        if (j < len && line[j] == '~')
                        {
                            // Too many ~ characters -> not strikethrough
                            res.Append("~~");
                            pos++;
                            continue;
                        }

                        // Check that there is no space after ~~ (e.g. ~~ text)
                        if (j < len && line[j] == ' ')
                        {
                            res.Append("~~");
                            pos++;
                            continue;
                        }

                        CntHl++;
                        res.Append("<del>");
                    }
                    else
                    {
                        // Closing strikethrough tag
                        CntHl--;
                        res.Append("</del>");
                    }

                    pos++; // skip the second '~' character
                    continue;
                }

                // Subscript
                else if (c == '~' && pos > 0 && pos + 1 < len && line[pos - 1] != ' ' && line[pos + 1] != ' ')
                {
                    if (CntSub == 0)
                    {
                        CntSub++;
                        res.Append("<sub>");
                        continue;
                    }
                    else
                    {
                        CntSub--;
                        res.Append("</sub>");
                        continue;
                    }
                }

                // Superscript
                else if (c == '^' && pos > 0 && pos + 1 < len && line[pos - 1] != ' ' && line[pos + 1] != ' ')
                {
                    if (CntSup == 0)
                    {
                        CntSup++;
                        res.Append("<sup>");
                        continue;
                    }
                    else
                    {
                        CntSup--;
                        res.Append("</sup>");
                        continue;
                    }
                }

                if (pos < len)
                {
                    res.Append(line[pos]);
                }
            }

            CloseUnclosedTags(Out);

            // Convert double spaces to non-breaking spaces
            string s = res.ToString();
            int k = s.IndexOf("  ");
            while (k > -1)
            {
                s = s.Replace("  ", "&nbsp; ");
                k = s.IndexOf("  ");
            }

            return s;
        }

        /// <summary>
        /// List of allowed inline HTML tags.
        /// </summary>
        private static readonly string[] AllowedInlineHtmlTags = { "sup", "sub", "span", "br" };

        /// <summary>
        /// Checks if the given HTML tag is allowed.
        /// </summary>
        private static bool IsAllowedHtmlTag(string tag)
        {
            // Parse the tag name (e.g., "sup", "/sup", "span", etc.)
            int i = 1; // skip '<'
            if (tag[i] == '/') { i++; }

            int start = i;
            while (i < tag.Length && (char.IsLetterOrDigit(tag[i]) || tag[i] == '-' || tag[i] == ':'))
            {
                i++;
            }

            string tagName = tag.Substring(start, i - start).ToLowerInvariant();

            if (!AllowedInlineHtmlTags.Contains(tagName))
            {
                return false;
            }

            // Check if the tag is properly closed
            return tag.EndsWith(">");
        }

        /// <summary>
        /// Parses inline code segments wrapped in backticks.
        /// </summary>
        private bool TryParseInlineCode(
            string line,
            int startIndex,
            out int endIndex,
            out string codeContent)
        {
            codeContent = null;
            endIndex = -1;

            if (line[startIndex] != '`') { return false; }

            // Count how many backticks we have at the start
            int fenceLength = 1;
            while (startIndex + fenceLength < line.Length && line[startIndex + fenceLength] == '`')
            {
                fenceLength++;
            }

            int i = startIndex + fenceLength;
            int contentStart = i;

            while (i < line.Length)
            {
                // Looking for the closing fence
                bool match = true;
                for (int j = 0; j < fenceLength && i + j < line.Length; j++)
                {
                    if (line[i + j] != '`')
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    endIndex = i + fenceLength - 1;
                    codeContent = line.Substring(contentStart, i - contentStart);
                    return true;
                }

                i++;
            }

            // If no closing backtick is found → not inline code
            return false;
        }

        /// <summary>
        /// Parses a Markdown link.
        /// </summary>
        private bool TryParseLink(string input, int startIndex, out string linkText, out string url, out string title, out int endIndex)
        {
            linkText = null;
            url = null;
            title = null;
            endIndex = startIndex;

            int i = startIndex + 1;
            int len = input.Length;

            // 1. Find the closing ']'
            int textEnd = -1;
            while (i < len)
            {
                if (input[i] == '\\') // Escape character
                {
                    i += 2; // Skip escaped character
                    continue;
                }
                else if (input[i] == ']')
                {
                    textEnd = i;
                    break;
                }
                i++;
            }
            if (textEnd == -1) { return false; } // No closing ']'

            linkText = input.Substring(startIndex + 1, textEnd - startIndex - 1);

            // 2. After ']' expect '('
            i = textEnd + 1;
            if (i >= len || input[i] != '(') { return false; }

            i++; // Skip '('

            // 3. Find the closing ')', detecting the URL and optional title
            // Format: url [optional whitespace] ["title"] [optional whitespace]

            // First find the end of the URL (up to whitespace or end of link)
            int urlStart = i;
            int urlEnd = -1;
            bool inTitle = false;
            bool titleStarted = false;
            int titleStart = -1;
            int titleEnd = -1;

            while (i < len)
            {
                if (input[i] == '\\')
                {
                    i += 2; // Skip escaped character
                    continue;
                }

                if (!inTitle)
                {
                    if (input[i] == ' ' || input[i] == '\t')
                    {
                        // Space between URL and title or end of URL
                        urlEnd = i;
                        // Potentially continues with title
                        while (i < len && (input[i] == ' ' || input[i] == '\t'))
                        {
                            i++;
                        }

                        if (i < len && (input[i] == '"' || input[i] == '\''))
                        {
                            inTitle = true;
                            titleStarted = true;
                            titleStart = i + 1;
                            i++;
                            continue;
                        }
                        else
                        {
                            // No title, look for closing ')'
                            while (i < len && input[i] != ')') { i++; }
                            break;
                        }
                    }
                    else if (input[i] == ')')
                    {
                        urlEnd = i;
                        break;
                    }
                }
                else
                {
                    // Inside title
                    if (input[i] == '"' || input[i] == '\'')
                    {
                        titleEnd = i;
                        i++;
                        // Find ')'
                        while (i < len && input[i] != ')') { i++; }
                        break;
                    }
                }
                i++;
            }

            if (urlEnd == -1) { return false; } // No closing ')'

            url = input.Substring(urlStart, urlEnd - urlStart).Trim();

            if (url.Contains('<') || url.Contains('>'))
            {
                ReportWarning("Invalid character in URL: " + url);
                url = url.Replace("<", "").Replace(">", "");
            }

            if (IsDangerousTag(url))
            {
                url = "";
            }

            if (titleStarted && titleStart != -1 && titleEnd != -1)
            {
                title = input.Substring(titleStart, titleEnd - titleStart);
            }

            endIndex = i < len ? i : len - 1;

            return true;
        }

        /// <summary>
        /// Tries to parse an auto-link or plain URL or email from the input string.
        /// </summary>
        private static bool TryParseAutoLink(string input, int startIndex, out string linkText, out string url, out string title, out string cls, out int endIndex)
        {
            linkText = null;
            url = null;
            title = null;
            endIndex = startIndex;

            int len = input.Length;

            // --- 1. Autolink: <https://example.com>, <user@example.com> ---
            if (input[startIndex] == '<')
            {
                int close = input.IndexOf('>', startIndex + 1);
                if (close != -1)
                {
                    string candidate = input.Substring(startIndex + 1, close - startIndex - 1).Trim();
                    if (IsValidEmail(candidate))
                    {
                        linkText = candidate;
                        url = "mailto:" + candidate;
                        cls = " class=\"email-link\"";
                        endIndex = close;
                        return true;
                    }
                    if (IsValidUrl(candidate))
                    {
                        linkText = candidate;
                        url = candidate;
                        cls = "";
                        endIndex = close;
                        return true;
                    }
                }
            }

            // --- 2. Plain URL: http://example.com, user@example.com ---
            if (IsUrlPrefix(input, startIndex) || IsEmailPrefix(input, startIndex))
            {
                int i = startIndex;
                while (i < len && !char.IsWhiteSpace(input[i]) && input[i] != ')')
                {
                    i++;
                }

                string candidate = input.Substring(startIndex, i - startIndex);
                if (IsValidEmail(candidate))
                {
                    linkText = candidate;
                    url = "mailto:" + candidate;
                    cls = " class=\"email-link\"";
                    endIndex = i - 1;
                    return true;
                }
                if (IsValidUrl(candidate))
                {
                    linkText = candidate;
                    url = candidate;
                    cls = "";
                    endIndex = i - 1;
                    return true;
                }
            }

            cls = "";
            return false;
        }

        /// <summary>
        /// Checks if the input string starts with a URL prefix.
        /// </summary>
        private static bool IsUrlPrefix(string input, int index)
        {
            return input.Substring(index).StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || input.Substring(index).StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || input.Substring(index).StartsWith("ftp://", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if the input string starts with a URL prefix.
        /// </summary>
        private static bool IsValidUrl(string candidate)
        {
            return Uri.TryCreate(candidate, UriKind.Absolute, out Uri uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeFtp);
        }

        /// <summary>
        /// Checks if the input string starts with an email prefix.
        /// </summary>
        private static bool IsEmailPrefix(string input, int index)
        {
            return input.Substring(index).Contains("@");
        }

        /// <summary>
        /// Checks if the input string is a valid email address.
        /// </summary>
        private static bool IsValidEmail(string input)
        {
            try
            {
                System.Net.Mail.MailAddress addr = new System.Net.Mail.MailAddress(input);
                return addr.Address == input;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Parses a Markdown image syntax.
        /// </summary>
        private static bool TryParseImage(string input, int startIndex, out string altText, out string url, out string title, out int endIndex)
        {
            altText = null;
            url = null;
            title = null;
            endIndex = startIndex;

            int len = input.Length;

            int i = startIndex + 2;

            // 1. Find closing ']'
            int altEnd = -1;
            while (i < len)
            {
                if (input[i] == '\\')
                {
                    i += 2;
                    continue;
                }
                if (input[i] == ']')
                {
                    altEnd = i;
                    break;
                }
                i++;
            }
            if (altEnd == -1) { return false; }

            altText = input.Substring(startIndex + 2, altEnd - (startIndex + 2));

            // 2. After ']' expect '('
            i = altEnd + 1;
            if (i >= len || input[i] != '(') { return false; }
            i++;

            // 3. Parse URL and title
            int urlStart = i;
            int urlEnd = -1;
            bool inTitle = false;
            bool titleStarted = false;
            int titleStart = -1;
            int titleEnd = -1;

            while (i < len)
            {
                if (input[i] == '\\')
                {
                    i += 2;
                    continue;
                }

                if (!inTitle)
                {
                    if (input[i] == ' ' || input[i] == '\t')
                    {
                        urlEnd = i;

                        // Skip to title
                        while (i < len && (input[i] == ' ' || input[i] == '\t')) { i++; }

                        if (i < len && (input[i] == '"' || input[i] == '\''))
                        {
                            inTitle = true;
                            titleStarted = true;
                            titleStart = i + 1;
                            i++;
                            continue;
                        }
                        else
                        {
                            // No title, go to closing ')'
                            while (i < len && input[i] != ')') { i++; }
                            break;
                        }
                    }
                    else if (input[i] == ')')
                    {
                        urlEnd = i;
                        break;
                    }
                }
                else
                {
                    if (input[i] == '"' || input[i] == '\'')
                    {
                        titleEnd = i;
                        i++;
                        // Go to closing ')'
                        while (i < len && input[i] != ')') { i++; }
                        break;
                    }
                }

                i++;
            }

            if (urlEnd == -1) { return false; }

            url = input.Substring(urlStart, urlEnd - urlStart).Trim();

            if (titleStarted && titleStart != -1 && titleEnd != -1)
            {
                title = input.Substring(titleStart, titleEnd - titleStart);
            }

            endIndex = i < len ? i : len - 1;
            return true;
        }

        /// <summary>
        /// Closes any unclosed tags in the HTML output, and reports warnings for each unclosed tag.
        /// This method ensures that all opened tags are properly closed before the end of the document.
        /// </summary>
        private void CloseUnclosedTags(StringBuilder sb)
        {
            while (CntItl > 0)
            {
                sb.Append("</em>");
                ReportWarning("Unclosed italic tag");
                CntItl--;
            }
            while (CntBld > 0)
            {
                sb.Append("</strong>");
                ReportWarning("Unclosed bold tag");
                CntBld--;
            }
            while (CntHl > 0)
            {
                sb.Append("</mark>");
                ReportWarning("Unclosed mark tag");
                CntHl--;
            }
            while (CntDel > 0)
            {
                sb.Append("</del>");
                ReportWarning("Unclosed del tag");
                CntDel--;
            }
        }

        #endregion

        #region Block recognition

        /// <summary>
        /// Determines if the given line is a horizontal rule in markdown format.
        /// </summary>
        private static bool IsHorizontalRule(string line)
        {
            string trim = line.Replace(" ", "");
            if (trim.Length < 3) { return false; }

            char c = trim[0];
            if (c != '-' && c != '*' && c != '_') { return false; }

            foreach (char ch in trim)
            {
                if (ch != c) { return false; }
            }

            return true;
        }

        /// <summary>
        /// Determines if the given line is a heading in markdown format.
        /// </summary>
        /// <remarks>
        /// A valid markdown heading starts with 1 to 6 consecutive '#' characters, followed by a space and then the heading content.
        /// </remarks>
        /// <param name="line">Line of text to evaluate.</param>
        /// <param name="level">
        /// When this method completes, contains the heading level (from 1 to 6) if the line is a heading; otherwise, 0.
        /// </param>
        /// <param name="content">When this method completes, contains the heading content (without leading '#' characters and space) if the line is a heading; otherwise, an empty string.</param>
        /// <returns><see langword="true"/> if the line is a valid markdown heading; otherwise, <see langword="false"/>.</returns>
        private bool ProcessHeading(string line, out int level, out string content, out string id)
        {
            level = 0;
            content = "";
            id = "";

            int i = 0;

            while (i < line.Length && line[i] == '#')
            {
                level++;
                i++;
            }

            if (level > 0 && level <= 6 && i < line.Length && line[i] == ' ')
            {
                content = ParseInlineStyles(line.Substring(i + 1).Trim());
                id = GenerateAnchorId(content);
                TocHeadings.Add(new HeadingInfo
                {
                    Level = level,
                    Text = content,
                    Id = id
                });

                return true;
            }

            return false;
        }

        /// <summary>
        /// Unordered lists (lists that use '-', '*', or '+' as item markers).
        /// </summary>
        private bool IsUnorderedList(
            string line,
            int indentSpc,
            int indentPos,
            out int level,
            out char bullet,
            out string content)
        {
            level = 0;
            bullet = '\0';
            content = "";

            char firstChar = line[indentPos];

            if (firstChar == '-' || firstChar == '*' || firstChar == '+')
            {
                indentPos++;

                // Must be a space or tab after the bullet character.
                if (indentPos < line.Length && (line[indentPos] == ' ' || line[indentPos] == '\t'))
                {
                    bullet = firstChar;

                    // List level is floor(indent / 2) + 1
                    level = (indentSpc / 2) + 1;

                    // Get the item text
                    content = ParseInlineStyles(line.Substring(indentPos + 1).Trim());

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Task list (a special type of unordered list that uses "[ ]" or "[x]" as item markers).
        /// </summary>
        private bool TryParseTaskList(string line, out string textWithoutCheckbox, out bool isChecked)
        {
            textWithoutCheckbox = null;
            isChecked = false;

            // It is assumed that the first two characters are already "- " or "* "
            if (line.Length < 6 || line[2] != '[') { return false; }

            char mark = line[3];
            if ((line.Length > 5 && line[5] != ' ') || line[4] != ']' || (mark != ' ' && mark != 'x' && mark != 'X'))
            {
                return false;
            }

            isChecked = mark == 'x' || mark == 'X';

            textWithoutCheckbox = line.Substring(6).Trim();

            return true;
        }

        /// <summary>
        /// Ordered lists (lists that use numbers followed by a dot as item markers).
        /// </summary>
        private bool IsOrderedList(
            string line,
            int indentSpc,
            int indentPos,
            out int level,
            out int number,
            out string content)
        {
            level = 0;
            number = 0;
            content = "";

            if (!char.IsDigit(line[indentPos])) { return false; }

            // Read number
            int start = indentPos;
            while (indentPos < line.Length && char.IsDigit(line[indentPos])) { indentPos++; }

            if (indentPos >= line.Length || line[indentPos] != '.') { return false; }

            string numberStr = line.Substring(start, indentPos - start);
            if (!int.TryParse(numberStr, out number)) { return false; }

            indentPos++; // skip dot

            // Must be space or tab
            if (indentPos >= line.Length || (line[indentPos] != ' ' && line[indentPos] != '\t')) { return false; }

            indentPos++; // skip space/tab

            // Item text
            content = ParseInlineStyles(line.Substring(indentPos).Trim());

            // Recalculate level
            level = (indentSpc / 2) + 1;

            return true;
        }

        /// <summary>
        /// Determines if the given line is a code fence in markdown format.
        /// </summary>
        private bool IsCodeFence(string trimLine, out string fence, out string language)
        {
            fence = null;
            language = null;

            if (string.IsNullOrWhiteSpace(trimLine)) { return false; }

            if (trimLine.StartsWith("```") || trimLine.StartsWith("~~~"))
            {
                fence = trimLine.Substring(0, 3);
                language = trimLine.Length > 3 ? trimLine.Substring(3).Trim() : "text";
                return true;
            }
            return false;
        }

        /// <summary>
        /// Processes raw HTML content in the markdown document.
        /// If the line starts with a '<', it is treated as raw HTML.
        /// </summary>
        private bool ProcessRawHtml(string line, string trimLine, out State state)
        {
            if (trimLine.StartsWith("<"))
            {
                int k = trimLine.IndexOf('>');
                if (k > 1)
                {
                    string tag = trimLine.Substring(0, k + 1);

                    if (IsDangerousTag(tag) || trimLine.StartsWith("<!-->"))
                    {
                        Out.Append(EscapeHtml(Lines[LineNum] + "\n"));
                        state = State.RawHtmlCode;
                        return true;
                    }
                    else
                    {
                        if (TryParseAutoLink(tag, 0, out string linkText, out string url, out string title, out string cls, out int _))
                        {
                            Out.Append($"<a href=\"{EscapeHtml(url)}{(title == null ? "" : $"\" title=\"{EscapeHtml(title)}")}\"{cls}>{linkText}</a>\n");
                            state = State.Paragraph;
                            return true;
                        }

                        if (IsSelfClosing(tag))
                        {
                            Out.Append(line + "\n");
                            state = State.RawHtmlCode;
                            return true;
                        }
                        else
                        {
                            string tagName = GetTagName(tag);
                            string endTag = $"</{tagName}>";

                            if (!trimLine.EndsWith(endTag) && !trimLine.StartsWith("<!-->"))
                            {
                                do
                                {
                                    Out.Append(Lines[LineNum] + "\n");
                                    if (++LineNum >= LinesCount) { break; }
                                } while (Lines[LineNum].IndexOf(endTag, StringComparison.OrdinalIgnoreCase) < 0);
                            }
                            if (LineNum < LinesCount) { Out.Append(Lines[LineNum] + "\n"); }

                            state = State.RawHtmlCode;
                            return true;
                        }
                    }
                }
            }
            state = State.Empty;
            return false;
        }

        /// <summary>
        /// Extracts the tag name from a given HTML or XML tag.
        /// </summary>
        private static string GetTagName(string tag)
        {
            int end = tag.IndexOfAny(new[] { ' ', '>' }, 1);
            if (end == -1) { end = tag.Length; }
            return tag.Substring(1, end - 1);
        }

        /// <summary>
        /// Set of known self-closing tags.
        /// </summary>
        private static readonly HashSet<string> SelfClosingTags = new HashSet<string> {
            "br", "img", "hr", "input", "link", "meta", "source", "track", "wbr",
            "area", "base", "col", "embed", "param", "command"
        };

        /// <summary>
        /// Determines whether the specified HTML or XML tag is self-closing.
        /// </summary>
        private static bool IsSelfClosing(string tag)
        {
            string name = GetTagName(tag).ToLowerInvariant();
            return tag.EndsWith("/>") || SelfClosingTags.Contains(name);
        }

        private static readonly HashSet<string> DangerTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "script", "iframe", "object", "embed", "svg", "math", "link", "meta"
        };

        private static readonly HashSet<string> DangerAttrPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "on" // onerror, onclick itd.
        };

        private static readonly string[] DangerousAttrValues = new[]
        {
            "javascript:", "data:text/html", "vbscript:", "expression("
        };

        /// <summary>
        /// Checks if the given HTML tag is dangerous. (XSS, phishing, etc.)
        /// </summary>
        private bool IsDangerousTag(string tagHtml)
        {
            if (string.IsNullOrWhiteSpace(tagHtml)) { return false; }

            Match tagNameMatch = Regex.Match(tagHtml, @"<\s*/?\s*([a-zA-Z0-9]+)", RegexOptions.IgnoreCase);
            if (!tagNameMatch.Success) { return false; }

            string tagName = tagNameMatch.Groups[1].Value;

            if (DangerTags.Contains(tagName))
            {
                ReportWarning($"Dangerous tag: {tagName}", inNextLine: true);
                return true;
            }

            Regex attrRegex = new Regex(@"([a-zA-Z0-9:-]+)\s*=\s*(""([^""]*)""|'([^']*)'|([^\s>]+))", RegexOptions.IgnoreCase);

            foreach (Match m in attrRegex.Matches(tagHtml))
            {
                string attrName = m.Groups[1].Value;
                string attrValue = m.Groups[3].Success
                    ? m.Groups[3].Value
                    : (m.Groups[4].Success ? m.Groups[4].Value : m.Groups[5].Value);

                // Decode HTML entities
                string htmlDecoded = WebUtility.HtmlDecode(attrValue);

                // Decode URL encoding
                // (Simple decoder sufficient for XSS validation. Not 100% the same as HttpUtility/WebUtility.UrlDecode.
                // E.g. "+" always replaced with space, whereas %2B decoded to "+".)
                string fullyDecoded = Uri.UnescapeDataString(htmlDecoded.Replace("+", " "));

                // Check attribute name
                if (DangerAttrPrefixes.Any(prefix => attrName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                {
                    ReportWarning("Dangerous attribute: " + attrName, inNextLine: true);
                    return true;
                }

                // Check attribute value
                if (attrName.Equals("href", StringComparison.OrdinalIgnoreCase) ||
                    attrName.Equals("src", StringComparison.OrdinalIgnoreCase) ||
                    attrName.Equals("style", StringComparison.OrdinalIgnoreCase))
                {
                    if (DangerousAttrValues.Any(d => fullyDecoded.Contains(d)))
                    {
                        ReportWarning("Dangerous attribute: " + attrName, inNextLine: true);
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        #region Footnotes processing

        // Footnote numbers and tracking
        private readonly Dictionary<string, int> FootnoteNumbers = new Dictionary<string, int>();

        private int NextFootnoteNumber = 1;

        // Set of used footnotes to avoid duplicates in the output
        private readonly HashSet<string> UsedFootnotes = new HashSet<string>();

        private Dictionary<string, string> FootnoteDefinitions { get; } = new Dictionary<string, string>();

        private const int FootnoteScanLines = 10;

        private bool FullScanFootnote = false;

        /// <summary>
        /// Parses footnotes in the markdown document.
        /// It scans the document starting from the specified line index and collects all footnote definitions.
        /// </summary>
        /// <param name="start">The line index to start scanning from.</param>
        private void ParseFootnotes(string[] lines, int start)
        {
            int len = lines.Length;
            for (int i = start; i < len; i++)
            {
                if (lines[i].StartsWith("[^") && lines[i].Contains("]:"))
                {
                    i = ParseFootnoteDefinition(lines, i);
                }
            }
        }

        /// <summary>
        /// Parses a footnote definition from the specified line.
        /// </summary>
        private int ParseFootnoteDefinition(string[] lines, int index)
        {
            string line = lines[index];
            int colonPos = line.IndexOf("]:");
            if (colonPos == -1) { return index; }

            string id = line.Substring(2, colonPos - 2).Trim();
            string content = line.Substring(colonPos + 2).Trim();

            StringBuilder sb = new StringBuilder(content);

            lines[index] = "";

            // Collect multi-line content (lines indented with 4 or more spaces or a tab)
            int len = lines.Length;
            int i = index;
            while (i + 1 < len && IsIndented(lines[i + 1]))
            {
                i++;
                sb.Append('\n' + lines[i].TrimStart());
                lines[i] = "";
            }

            FootnoteDefinitions[id] = sb.ToString();
            return i;
        }

        private bool IsIndented(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) { return false; }
            return line.StartsWith("    ") || line.StartsWith("\t");
        }

        #endregion

        #region Table of Contents (TOC) processing

        private readonly List<HeadingInfo> TocHeadings = new List<HeadingInfo>();

        private class HeadingInfo
        {
            public int Level;   // e.g. 1 for <h1>, 2 for <h2>...
            public string Text; // text from the heading
            public string Id;   // HTML id (anchor)
        }

        /// <summary>
        /// Generates an anchor ID from the given text.
        /// The ID is a lowercase version of the text with spaces replaced by dashes and non-alphanumeric characters removed.
        /// </summary>
        private static string GenerateAnchorId(string text)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in text.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c) || c == '-')
                {
                    sb.Append(c);
                }
                else if (char.IsWhiteSpace(c))
                {
                    sb.Append('-');
                }
            }
            return sb.ToString().Trim('-');
        }

        /// <summary>
        /// Generates a Table of Contents (TOC) from the collected headings.
        /// </summary>
        private string GenerateToc(List<HeadingInfo> headings)
        {
            if (headings.Count == 0) { return ""; }

            StringBuilder sb = new StringBuilder();
            int prevLevel = headings[0].Level;
            int indentLevel = 0;
            bool openLi = false;

            sb.Append("\n<!-- Table of Contents -->\n" + Indent(indentLevel) + "<ul>\n");
            indentLevel++;

            for (int i = 0; i < headings.Count; i++)
            {
                HeadingInfo heading = headings[i];
                int currentLevel = heading.Level;

                // If going deeper: open <ul> inside <li>, without closing <li>
                if (currentLevel > prevLevel)
                {
                    sb.Append("\n" + Indent(indentLevel) + "<ul>\n");
                    indentLevel++;
                }

                // If returning to a shallower level: close excess <li> and <ul>
                else if (currentLevel < prevLevel)
                {
                    if (openLi)
                    {
                        sb.Append(Indent(indentLevel) + "</li>\n");
                    }
                    for (int j = prevLevel; j > currentLevel; j--)
                    {
                        indentLevel--;
                        sb.Append(Indent(indentLevel) + "</ul>\n");
                        sb.Append(Indent(indentLevel) + "</li>\n");
                    }
                }

                // Same level → close previous <li>
                else if (openLi)
                {
                    sb.Append(Indent(indentLevel) + "</li>\n");
                }

                // Open new <li> (do not close yet if a nested <ul> is coming)
                sb.Append(Indent(indentLevel) + $"<li><a href=\"#{heading.Id}\">{heading.Text}</a>");
                openLi = true;

                prevLevel = currentLevel;
            }

            // Close the last <li> if it is still open
            if (openLi) { sb.Append("</li>\n"); }

            // Close all remaining open <ul> and <li>
            for (int i = prevLevel; i > headings[0].Level && indentLevel > 1; i--)
            {
                indentLevel--;
                sb.Append(Indent(indentLevel) + "</ul>\n");
                sb.Append(Indent(indentLevel) + "</li>\n");
            }

            indentLevel--;
            sb.Append(Indent(indentLevel) + "</ul>");
            return sb.ToString();
        }

        private string Indent(int level)
        {
            return new string(' ', level * 2); // 2 spaces per level
        }

        #endregion

        #region Table processing

        private string ParseTable(string[] lines, int startLineIndex, out int linesConsumed)
        {
            linesConsumed = 0;

            int current = startLineIndex;
            if (current >= lines.Length) { return null; }

            // Parse header
            string headerLine = lines[current];
            List<string> headerCells = ParseTableRow(headerLine);
            current++;

            if (current >= lines.Length) { return null; }

            // Parse separator
            string separatorLine = lines[current];
            if (!IsTableHeaderSeparator(separatorLine)) { return null; }

            List<string> alignments = ParseAlignments(separatorLine, headerCells.Count);
            current++;

            StringBuilder sb = new StringBuilder();
            sb.Append("<table>\n");

            // Write thead
            sb.Append("  <thead>\n");
            sb.Append("    <tr>\n");
            for (int i = 0; i < headerCells.Count; i++)
            {
                string align = alignments[i];
                string style = string.IsNullOrEmpty(align) ? "" : $" style=\"text-align:{align}\"";
                sb.Append($"      <th{style}>{ParseInlineStyles(headerCells[i])}</th>\n");
            }
            sb.Append("    </tr>\n");
            sb.Append("  </thead>\n");

            // Write tbody
            sb.Append("  <tbody>\n");
            while (current < lines.Length && lines[current].Contains("|"))
            {
                List<string> rowCells = ParseTableRow(lines[current]);
                sb.Append("    <tr>\n");
                for (int i = 0; i < headerCells.Count; i++)
                {
                    string cell = i < rowCells.Count ? rowCells[i] : "";
                    string align = alignments[i];
                    string style = string.IsNullOrEmpty(align) ? "" : $" style=\"text-align:{align}\"";
                    sb.Append($"      <td{style}>{ParseInlineStyles(cell)}</td>\n");
                }
                sb.Append("    </tr>\n");
                current++;
            }
            sb.Append("  </tbody>\n");

            sb.Append("</table>\n");

            linesConsumed = current - startLineIndex;
            return sb.ToString();
        }

        private static bool IsTableHeaderSeparator(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) { return false; }

            bool hasPipe = false;
            bool hasDash = false;

            foreach (char c in line)
            {
                if (c == '|') { hasPipe = true; }
                else if (c == '-') { hasDash = true; }
                else if (c != ':' && !char.IsWhiteSpace(c)) { return false; }
            }

            return hasPipe && hasDash;
        }

        private static List<string> ParseTableRow(string line)
        {
            List<string> cells = new List<string>();

            foreach (string part in line.Split('|'))
            {
                string trimmed = part.Trim();
                if (trimmed.Length > 0) { cells.Add(trimmed); }
            }

            return cells;
        }

        private static List<string> ParseAlignments(string line, int expectedCount)
        {
            List<string> aligns = new List<string>();
            foreach (string part in line.Split('|'))
            {
                string trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed))
                { continue; }

                if (trimmed.StartsWith(":") && trimmed.EndsWith(":"))
                {
                    aligns.Add("center");
                }
                else if (trimmed.StartsWith(":"))
                {
                    aligns.Add("left");
                }
                else if (trimmed.EndsWith(":"))
                {
                    aligns.Add("right");
                }
                else
                {
                    aligns.Add("");
                }
            }

            // If there are fewer alignments than columns, fill with empty strings
            while (aligns.Count < expectedCount)
            {
                aligns.Add("");
            }

            return aligns;
        }

        #endregion

        #region Warnings processing

        /// <summary>
        /// List of warnings encountered during Markdown parsing.
        /// This list is used to collect warnings about potential issues in the Markdown text,
        /// such as unclosed tags or incorrect formatting.
        /// </summary>
        private readonly List<string> Warnings = new List<string>();

        /// <summary>
        /// Reports a warning encountered during Markdown parsing.
        /// </summary>
        /// <param name="desc">Description of the warning.</param>
        private void ReportWarning(string desc, bool inNextLine = false)
        {
            Warnings.Add($"Line {(inNextLine ? LineNum + 1 : LineNum)}: {EscapeHtml(desc)}");
        }

        /// <summary>
        /// Generates a report of any warnings encountered during Markdown parsing.
        /// </summary>
        private void GenerateWarningsReport()
        {
            // Generate a report of any warnings
            if (Warnings.Count > 0)
            {
                Out.Append(
                    "\n<hr>\n" +
                    "<div class=\"warnings\" style=\"background: #f5f78a; border:2px solid #c43f0f; padding:0.5em; color: #000333; font-family:monospace; font-size:0.95em;\">\n" +
                    "  <h2>⚠️ WARNINGS</h2>\n" +
                    "  <ul>\n");
                foreach (string desc in Warnings)
                {
                    Out.Append($"    <li>{desc}</li>\n");
                }
                Out.Append("  </ul>\n</div>\n");
            }
        }

        #endregion
    }
}
