using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace m.format.conv
{
    /// <summary>
    /// Converts Markdown documents.
    /// </summary>
    /// <version>1.0.0</version>
    /// <date>2025-07-05</date>
    /// <author>Miloš Perunović</author>
    public class Markdown
    {
        #region Main methods for converting Markdown to HTML

        /// <summary>
        /// Converts Markdown document to HTML.
        /// </summary>
        /// <param name="doc">The Markdown document as a string.</param>
        /// <param name="lang">Language code (e.g. "en", "cnr")</param>
        /// <param name="head">Additional head elements (e.g. CSS links)</param>
        /// <returns>HTML representation of the markdown</returns>
        public static string ToHtml(string doc, string lang = "en", string head = null)
        {
            Markdown md = new Markdown();
            string body = md.ToHtmlBody(doc, out Dictionary<string, string> metadata);

            // Generate html meta tags from metadata
            StringBuilder meta = new StringBuilder();
            if (!metadata.ContainsKey("title")) { metadata["title"] = "Untitled Document"; }
            foreach (KeyValuePair<string, string> pair in metadata)
            {
                string key = pair.Key.ToLowerInvariant();

                if (key == "title")
                {
                    meta.Append($"<title>{WebUtility.HtmlEncode(pair.Value)}</title>\n");
                }
                else
                {
                    meta.Append($"<meta name=\"{WebUtility.HtmlEncode(pair.Key)}\" content=\"{WebUtility.HtmlEncode(pair.Value)}\">\n");
                }
            }

            // Generate the HTML document
            return
                "<!DOCTYPE html>\n" +
                $"<html lang=\"{lang}\">\n" +
                "<head>\n" +
                (meta.Length > 0 ? meta.ToString() : "") +
                (head ?? "") +
                "</head>\n" +
                "<body>\n" +
                $"{body}" +
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
            HorizontalRule
        }

        /// <summary>
        /// Lines of markdown text.
        /// </summary>
        private string[] Lines;

        /// <summary>
        /// HTML body content.
        /// This is where the converted HTML will be stored.
        /// </summary>
        private readonly StringBuilder Body = new StringBuilder("");

        /// <summary>
        /// Current state of Markdown parsing.
        /// This is used to track the current context while parsing the Markdown text.
        /// </summary>
        private State CurrState;

        private State PrevState = State.Empty;

        // List states
        private State ListState = State.Empty;

        private int ListLastLevel;

        private string CodeFenceSeq;

        /// <summary>
        /// Stack for closing tags.
        /// This is used to keep track of the tags that need to be closed when leaving a list or block context.
        /// </summary>
        private readonly Queue ListClosingTags = new Queue(10);

        private static string EscapeHtml(string input) => input.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        /// <summary>
        /// Converts Markdown document to HTML.
        /// </summary>
        /// <param name="doc">Markdown document</param>
        /// <param name="metadata">Document metadata. This includes title, author, date,...</param>
        /// <returns>HTML representation of the markdown</returns>
        public string ToHtmlBody(string doc, out Dictionary<string, string> metadata)
        {
            Stopwatch sw = Stopwatch.StartNew();

            Lines = doc.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            int linesCount = Lines.Length;
            metadata = new Dictionary<string, string>();

            int startLine = 0;
            int emptyCnt = 0;

            // Detect YAML Front Matter
            if (linesCount > 0 && Lines[0].Trim() == "---")
            {
                int end = Array.FindIndex(Lines, 1, l => l.Trim() == "---");
                if (end > 0)
                {
                    for (int i = 1; i < end; i++)
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
                                case "date":
                                    metadata["date"] = value;
                                    break;
                            }
                        }
                    }
                    startLine = end;
                }
            }

            ParseFootnotes(Lines, Math.Max(0, linesCount - FootnoteScanLines));

            for (int lineNum = startLine; lineNum < linesCount; lineNum++)
            {
                string line = Lines[lineNum];
                string trimLine = line.Trim();

                // Empty line
                if (trimLine.Length == 0)
                {
                    CurrState = State.Empty;
                    emptyCnt++;
                    if (PrevState == State.Paragraph)
                    {
                        Body.Append("</p>\n");
                    }
                    else if (PrevState == State.UnorderedList || PrevState == State.TaskList || PrevState == State.OrderedList || PrevState == State.Blockquote)
                    {
                        CurrState = PrevState;
                    }
                    else if (PrevState == State.Empty && emptyCnt == 3)
                    {
                        Body.Append("<br>\n");
                        emptyCnt = 1;
                    }
                }

                // Unordered list, task lists
                else if (IsUnorderedList(line, out _, out int level, out _, out string content))
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
                        for (int j = 0; j < level - ListLastLevel; j++)
                        {
                            line = $"<ul>\n{ind}<li>{inputBox}{content}";
                            ListClosingTags.Enqueue("</ul>\n");
                        }
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
                else if (IsOrderedList(line, out _, out level, out _, out content))
                {
                    CurrState = State.OrderedList;
                    ListState = State.OrderedList;
                    CloseBlock();

                    string ind = new string(' ', level * 2);

                    if (level > ListLastLevel)
                    {
                        for (int j = 0; j < level - ListLastLevel; j++)
                        {
                            line = $"<ol>\n{ind}<li>{content}";
                            ListClosingTags.Enqueue("</ol>\n");
                        }
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
                    line = "<hr>\n";
                }

                // Heading
                else if (IsHeading(line, out level, out content))
                {
                    CurrState = State.Heading;
                    CloseBlock();
                    line = $"<h{level}>{content}</h{level}>\n";
                }

                // Blockquote
                else if (IsBlockquote(trimLine, out content))
                {
                    CurrState = State.Blockquote;
                    if (PrevState != State.Blockquote)
                    {
                        CloseBlock();
                        Body.Append("<blockquote>\n");
                    }
                    line = content.Length == 0 ? "" : $"<p>{content}</p>\n";
                }

                // Code block
                else if (IsCodeFence(trimLine, out string fence, out string lang))
                {
                    CurrState = State.CodeBlock;
                    CodeFenceSeq = fence;
                    if (PrevState != State.CodeBlock)
                    {
                        CloseBlock();
                        Body.Append($"<pre><code class=\"{lang}\">\n");
                        bool endBlock = false;
                        while (!endBlock && ++lineNum < linesCount)
                        {
                            line = Lines[lineNum];
                            if (IsFenceClosingLine(line, CodeFenceSeq))
                            {
                                Body.Append("</code></pre>\n");
                                endBlock = true;
                            }
                            else
                            {
                                Body.Append($"{EscapeHtml(line)}\n");
                            }
                        }
                    }
                    CurrState = State.Empty;
                    line = "";
                }

                // Raw HTML
                else if (trimLine.StartsWith("<"))
                {
                    int k = trimLine.IndexOf('>');
                    if (k > 1)
                    {
                        CurrState = State.RawHtmlCode;
                        string tag = trimLine.Substring(0, k + 1);

                        if (IsSelfClosing(tag))
                        {
                            Body.AppendLine(line);
                            continue;
                        }
                        else
                        {
                            string tagName = GetTagName(tag);
                            string endTag = "</" + tagName + ">";

                            do
                            {
                                Body.AppendLine(Lines[lineNum]);
                                if (++lineNum >= linesCount) { break; }
                            } while (Lines[lineNum].IndexOf(endTag, StringComparison.OrdinalIgnoreCase) < 0);

                            if (lineNum < linesCount) { Body.AppendLine(Lines[lineNum]); }

                            continue;
                        }
                    }
                    else
                    {
                        CurrState = State.Paragraph;
                        line = ParseInlineStyles(line);
                    }
                }

                // Tables
                else if (trimLine.StartsWith("|") && lineNum < linesCount - 2 && IsTableHeaderSeparator(Lines[lineNum + 1]))
                {
                    CurrState = State.Table;
                    CloseBlock();
                    string table = ParseMarkdownTable(Lines, lineNum, out int linesConsumed);
                    Body.Append(table);
                    line = "";
                    lineNum = linesConsumed + lineNum - 1;
                }

                // Paragraph
                else
                {
                    CurrState = State.Paragraph;
                    CloseBlock();
                    line = ParseInlineStyles(line);
                }

                PrevState = CurrState;

                // Adding line to HTML
                if (CurrState != State.Empty)
                {
                    emptyCnt = 0;
                    Body.Append(line);
                }
            }

            if (usedFootnotes.Count > 0)
            {
                Body.Append("<div class=\"footnotes\">\n<ol>\n");

                foreach (string id in usedFootnotes)
                {
                    if (!FootnoteDefinitions.TryGetValue(id, out string text))
                    {
                        continue; // footnote is not defined, skip
                    }

                    // Create footnote with link back to reference
                    Body.Append($"<li id=\"fn{id}\">{ParseInlineStyles(text)} <a href=\"#ref{id}\" class=\"footnote-backref\">↩</a></li>\n");
                }

                Body.Append("</ol>\n</div>\n");
            }

            sw.Stop();
            double seconds = (double)sw.ElapsedTicks / Stopwatch.Frequency;
            Console.WriteLine("Markdown -> HTML conv.: " + seconds);

            return Body.ToString();
        }

        // Set of known self-closing tags
        private static readonly HashSet<string> selfClosingTags = new HashSet<string> {
            "br", "img", "hr", "input", "link", "meta", "source", "track", "wbr",
            "area", "base", "col", "embed", "param", "command"
        };

        /// <summary>
        /// Determines whether the specified HTML or XML tag is self-closing.
        /// </summary>
        private static bool IsSelfClosing(string tag)
        {
            string name = GetTagName(tag).ToLowerInvariant();
            return tag.EndsWith("/>") || selfClosingTags.Contains(name);
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
        /// Closes the current block in the HTML document.
        /// </summary>
        private void CloseBlock()
        {
            if (!(CurrState == State.UnorderedList || CurrState == State.TaskList || CurrState == State.OrderedList) && ListState != State.Empty)
            {
                ListLevelDown(0);
            }

            string line = "";

            if (PrevState == State.Blockquote)
            {
                Body.Append("</blockquote>\n");
            }

            if (CurrState == State.Paragraph)
            {
                line = PrevState != State.Paragraph ? "\n<p>" : " ";
            }
            else if (PrevState == State.Paragraph)
            {
                line = "</p>\n";
            }

            if (line.Length > 0) { Body.Append(line); }
        }

        /// <summary>
        /// Closes list levels in the current document structure down to the specified level.
        /// </summary>
        /// <param name="level">
        /// The target list level to close down to. Must be less than or equal to the current list level.
        /// </param>
        private void ListLevelDown(int level)
        {
            Body.Append("</li>\n");

            for (int j = 0; j < ListLastLevel - level; j++)
            {
                string tag = ListClosingTags.Dequeue().ToString();
                Body.Append(tag);
            }

            if (level == 0)
            {
                ListState = State.Empty;
                ListLastLevel = 0;
            }
        }

        #endregion

        #region Inline conversions

        /// <summary>
        /// Parses a line of text and converts inline style markers (e.g., bold, italic) into corresponding HTML tags.
        /// </summary>
        /// <returns>
        /// A string where inline style markers (e.g., <c>*</c> for bold and italic) are replaced with their
        /// corresponding HTML tags (e.g., <c>&lt;strong&gt;</c> and <c>&lt;em&gt;</c>).
        /// </returns>
        private string ParseInlineStyles(string line)
        {
            StringBuilder sb = new StringBuilder();

            int len = line.Length;

            int bld = 0, itl = 0;

            for (int i = 0; i < len; i++)
            {
                char c = line[i];
                bool skip = false;

                // Escape character
                if (c == '\\' && i + 1 < len)
                {
                    i++;
                }
                // Line break
                else if (c == '\n')
                {
                    sb.Append("<br>");
                }
                // HTML entity
                else if (c == '&')
                {
                    int semicolonPos = line.IndexOf(';', i + 1);
                    if (semicolonPos > i)
                    {
                        string entity = line.Substring(i, semicolonPos - i + 1);
                        string decoded = WebUtility.HtmlDecode(entity);
                        if (decoded != entity)
                        {
                            sb.Append(entity);
                            i += entity.Length - 1;
                            skip = true;
                        }
                    }
                    if (!skip)
                    {
                        sb.Append("&amp;");
                        skip = true;
                    }
                }
                // Less-than sign
                else if (c == '<')
                {
                    sb.Append("&lt;");
                    skip = true;
                }
                // Greater-than sign
                else if (c == '>')
                {
                    sb.Append("&gt;");
                    skip = true;
                }

                // Footnotes and links
                else if (c == '[')
                {
                    // Footnotes
                    if (line[i] == '[' && i + 1 < line.Length && line[i + 1] == '^')
                    {
                        int end = line.IndexOf(']', i);
                        if (end > -1)
                        {
                            string id = line.Substring(i + 2, end - i - 2);

                            if (!FullScanFootnote && !FootnoteDefinitions.ContainsKey(id))
                            {
                                // Re-parse footnotes in case some definitions are not at the end of the document
                                ParseFootnotes(Lines, 0);
                                FullScanFootnote = true;
                            }

                            if (FootnoteDefinitions.ContainsKey(id))
                            {
                                usedFootnotes.Add(id);

                                string tooltip = "";

                                // Add or get the existing footnote number
                                if (!footnoteNumbers.TryGetValue(id, out int number))
                                {
                                    number = nextFootnoteNumber++;
                                    footnoteNumbers[id] = number;
                                    tooltip = FootnoteDefinitions[id].Replace("\"", "&quot;");
                                }

                                // HTML link in the text uses href to "fn" and id "ref"
                                sb.Append($"<a href=\"#fn{id}\" id=\"ref{id}\" title=\"{tooltip}\">[{number}]</a>");

                                i = end; // skip the parsed part
                                continue;
                            }
                        }
                    }
                    // Links
                    else if (TryParseLink(line, i, out string linkText, out string url, out string title, out int endIndex))
                    {
                        i = endIndex;
                        sb.Append($"<a href=\"{EscapeHtml(url)}{(title == null ? "" : $"\" title=\"{EscapeHtml(title)}")}\">{linkText}</a>");
                        skip = true;
                    }
                }

                // Image
                else if (c == '!' && i + 1 < len && line[i + 1] == '[')
                {
                    if (TryParseImage(line, i, out string altText, out string url, out string title, out int endIndex))
                    {
                        i = endIndex + 1;
                        sb.Append($"<img src=\"{url}\" alt=\"{altText}\"{(title == null ? "" : $"\" title=\"{title}")}>");
                    }
                }

                // Inline code
                else if (c == '`')
                {
                    if (TryParseInlineCode(line, i, out int endIndex, out string codeContent))
                    {
                        // HTML-escape the content
                        string html = $"<code>{WebUtility.HtmlEncode(codeContent)}</code>";

                        // Add the HTML to the result
                        sb.Append(html);

                        i = endIndex; // skip the parsed part
                        continue;
                    }
                }

                // Basic styles
                else if (c == '*')
                {
                    string sty;
                    char c2 = (i + 1 < len) ? line[i + 1] : '\0';
                    char c3 = (i + 2 < len) ? line[i + 2] : '\0';

                    // Bold + Italic
                    if (c2 == '*' && c3 == '*')
                    {
                        i += 3;
                        if (bld == 0 && itl == 0 && c2 != ' ')
                        {
                            bld++; itl++; sty = "<strong><em>";
                        }
                        else
                        {
                            bld--; itl--; sty = "</em></strong>";
                        }
                    }

                    // Bold
                    else if (c2 == '*')
                    {
                        i += 2;
                        if (bld == 0 && c2 != ' ')
                        {
                            bld++; sty = "<strong>";
                        }
                        else if (bld > 0)
                        {
                            bld--; sty = "</strong>";
                        }
                        else
                        {
                            i -= 2;
                            sty = "<b>[ERR: Bold tag]</b>"; // Incorrectly written character for "bold"
                        }
                    }

                    // Italic
                    else
                    {
                        i++;
                        if (itl == 0 && c2 != ' ')
                        {
                            itl++; sty = "<em>";
                        }
                        else if (itl > 0)
                        {
                            itl--; sty = "</em>";
                        }
                        else
                        {
                            i--;
                            sty = "<b>[ERR: Italic tag]</b>"; // Incorrectly written character for "italic"
                        }
                    }

                    sb.Append(sty);
                }

                if (!skip && i < len)
                {
                    sb.Append(line[i]);
                }
            }

            // Fix unclosed tags from the markdown document
            while (itl > 0)
            {
                sb.Append("</em>[ERR: Unclosed italic tag]");
                itl--;
            }
            while (bld > 0)
            {
                sb.Append("</em>[ERR: Unclosed bold tag]");
                bld--;
            }

            // Double "space" characters
            string s = sb.ToString();
            int k = -1;
            do
            {
                s = s.Replace("  ", "&nbsp; ");
                k = s.IndexOf("  ", k + 1);
            } while (k > -1);

            return s;
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
        private static bool TryParseLink(string input, int startIndex, out string linkText, out string url, out string title, out int endIndex)
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

            if (titleStarted && titleStart != -1 && titleEnd != -1)
            {
                title = input.Substring(titleStart, titleEnd - titleStart);
            }

            endIndex = i < len ? i : len - 1;

            return true;
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
        private bool IsHeading(string line, out int level, out string content)
        {
            level = 0;
            content = "";

            int i = 0;

            while (i < line.Length && line[i] == '#')
            {
                level++;
                i++;
            }

            if (level > 0 && level <= 6 && i < line.Length && line[i] == ' ')
            {
                content = ParseInlineStyles(line.Substring(i + 1).Trim());
                return true;
            }

            return false;
        }

        /// <summary>
        /// Unordered lists (lists that use '-', '*', or '+' as item markers).
        /// </summary>
        public bool IsUnorderedList(
            string line,
            out int indent,
            out int level,
            out char bullet,
            out string content)
        {
            indent = 0;
            level = 0;
            bullet = '\0';
            content = "";

            if (string.IsNullOrWhiteSpace(line)) { return false; }

            // Count leading spaces/tabs
            int i = 0;
            while (i < line.Length && (line[i] == ' ' || line[i] == '\t'))
            {
                indent += (line[i] == '\t') ? 4 : 1;
                i++;
            }

            if (i >= line.Length) { return false; }

            char firstChar = line[i];

            if (firstChar == '-' || firstChar == '*' || firstChar == '+')
            {
                i++;

                // Must be a space or tab after the bullet character.
                if (i < line.Length && (line[i] == ' ' || line[i] == '\t'))
                {
                    bullet = firstChar;

                    // List level is floor(indent / 2) + 1
                    level = (indent / 2) + 1;

                    // Get the item text
                    content = ParseInlineStyles(line.Substring(i + 1).Trim());

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Task list (a special type of unordered list that uses "[ ]" or "[x]" as item markers).
        /// </summary>
        public bool TryParseTaskList(string line, out string textWithoutCheckbox, out bool isChecked)
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
        public bool IsOrderedList(
            string line,
            out int indent,
            out int level,
            out int number,
            out string content)
        {
            indent = 0;
            level = 0;
            number = 0;
            content = "";

            if (string.IsNullOrWhiteSpace(line)) { return false; }

            // Count leading spaces/tabs
            int i = 0;
            while (i < line.Length && (line[i] == ' ' || line[i] == '\t'))
            {
                indent += (line[i] == '\t') ? 4 : 1;
                i++;
            }

            if (i >= line.Length || !char.IsDigit(line[i])) { return false; }

            // Read number
            int start = i;
            while (i < line.Length && char.IsDigit(line[i])) { i++; }

            if (i >= line.Length || line[i] != '.') { return false; }

            string numberStr = line.Substring(start, i - start);
            if (!int.TryParse(numberStr, out number)) { return false; }

            i++; // skip dot

            // Must be space or tab
            if (i >= line.Length || (line[i] != ' ' && line[i] != '\t')) { return false; }

            i++; // skip space/tab

            // Item text
            content = ParseInlineStyles(line.Substring(i).Trim());

            // Recalculate level
            level = (indent / 2) + 1;

            return true;
        }

        /// <summary>
        /// Determines if the given line is a blockquote in markdown format.
        /// </summary>
        public bool IsBlockquote(string line, out string content)
        {
            content = "";
            if (line.StartsWith(">"))
            {
                content = ParseInlineStyles(line.Substring(1).TrimStart());
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines if the given line is a code fence in markdown format.
        /// </summary>
        private bool IsCodeFence(string line, out string fence, out string language)
        {
            fence = null;
            language = null;

            if (string.IsNullOrWhiteSpace(line)) { return false; }

            if (line.StartsWith("```") || line.StartsWith("~~~"))
            {
                int firstSpace = line.IndexOf(' ');
                if (firstSpace == -1)
                {
                    fence = line;
                }
                else
                {
                    fence = line.Substring(0, firstSpace);
                    language = line.Substring(firstSpace + 1).Trim();
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines if the given line is a closing fence for a code block.
        /// </summary>
        private bool IsFenceClosingLine(string line, string fence)
        {
            if (line == null) { return false; }

            line = line.TrimEnd(); // Remove whitespace from the right side only

            if (!line.StartsWith(fence)) { return false; }

            // Extract the part of the line after the fence string
            string afterFence = line.Substring(fence.Length);

            // Check if afterFence contains only whitespace
            return string.IsNullOrWhiteSpace(afterFence);
        }

        #endregion

        #region Footnotes

        // Footnote numbers and tracking
        private readonly Dictionary<string, int> footnoteNumbers = new Dictionary<string, int>();

        private int nextFootnoteNumber = 1;

        // Set of used footnotes to avoid duplicates in the output
        private readonly HashSet<string> usedFootnotes = new HashSet<string>();

        public Dictionary<string, string> FootnoteDefinitions { get; } = new Dictionary<string, string>();

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

        #region Parsing markdown table

        private static string ParseMarkdownTable(string[] lines, int startLineIndex, out int linesConsumed)
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
            sb.AppendLine("<table>");

            // Write thead
            sb.AppendLine("  <thead>");
            sb.AppendLine("    <tr>");
            for (int i = 0; i < headerCells.Count; i++)
            {
                string align = alignments[i];
                string style = string.IsNullOrEmpty(align) ? "" : $" style=\"text-align:{align}\"";
                sb.AppendLine($"      <th{style}>{EscapeHtml(headerCells[i])}</th>");
            }
            sb.AppendLine("    </tr>");
            sb.AppendLine("  </thead>");

            // Write tbody
            sb.AppendLine("  <tbody>");
            while (current < lines.Length && lines[current].Contains("|"))
            {
                List<string> rowCells = ParseTableRow(lines[current]);
                sb.AppendLine("    <tr>");
                for (int i = 0; i < headerCells.Count; i++)
                {
                    string cell = i < rowCells.Count ? rowCells[i] : "";
                    string align = alignments[i];
                    string style = string.IsNullOrEmpty(align) ? "" : $" style=\"text-align:{align}\"";
                    sb.AppendLine($"      <td{style}>{EscapeHtml(cell)}</td>");
                }
                sb.AppendLine("    </tr>");
                current++;
            }
            sb.AppendLine("  </tbody>");

            sb.AppendLine("</table>");

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
    }
}
