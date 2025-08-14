using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace m.format.conv
{
    /// <summary>
    /// Converts HTML to Markdown.
    /// </summary>
    /// <version>2.3.2</version>
    /// <date>2025-08-14</date>
    /// <author>Miloš Perunović</author>
    public class ConvHtmlMarkdown
    {
        #region Main methods for converting HTML to Markdown

        /// <summary>
        /// Converts HTML document to Markdown.
        /// </summary>
        /// <param name="html">The HTML document as a string.</param>
        /// <param name="ignoreWarnings">Whether to ignore warnings during conversion</param>
        /// <param name="markCodeBlock">Whether to mark code blocks</param>
        /// <returns>Markdown representation of the HTML document</returns>
        public static string Convert(string html, bool ignoreWarnings = false, bool markCodeBlock = true)
        {
            return new ConvHtmlMarkdown().ToMarkdownBody(html, ignoreWarnings, markCodeBlock, convertTxt: false);
        }

        /// <summary>
        /// Flag indicating whether to use smart plain text conversion.
        /// </summary>
        private bool IsTxtFormat;

        /// <summary>
        /// Converts HTML document to plain text.
        /// </summary>
        /// <param name="html">The HTML document as a string.</param>
        /// <param name="ignoreWarnings">Whether to ignore warnings during conversion</param>
        /// <returns>Plain text representation of the HTML document</returns>
        public static string ConvertToTxt(string html, bool ignoreWarnings = false)
        {
            return new ConvHtmlMarkdown().ToMarkdownBody(html, ignoreWarnings, markCodeBlock: true, convertTxt: true);
        }

        /// <summary>
        /// StringBuilder for accumulating the Markdown output.
        /// This is used to build the final Markdown string efficiently.
        /// </summary>
        private StringBuilder Out;

        /// <summary>
        /// Current line number.
        /// </summary>
        private int LineNum = 1;

        /// <summary>
        /// StringBuilder for accumulating the text content.
        /// This is used to collect text between HTML tags before processing.
        /// </summary>
        private readonly StringBuilder TextBuffer = new StringBuilder();

        /// <summary>
        /// Flag indicating whether to mark code blocks.
        /// </summary>
        private bool MarkCodeBlock;

        /// <summary>
        /// Converts HTML document to Markdown.
        /// </summary>
        /// <param name="html">The HTML document as a string.</param>
        /// <param name="ignoreWarnings">Whether to ignore warnings during conversion</param>
        /// <param name="markCodeBlock">Whether to mark code blocks</param>
        /// <param name="convertTxt">Whether to convert to plain text format</param>
        /// <returns>Markdown representation of the HTML document</returns>
        private string ToMarkdownBody(string html, bool ignoreWarnings, bool markCodeBlock, bool convertTxt)
        {
            MarkCodeBlock = markCodeBlock;
            IsTxtFormat = convertTxt;

            Stopwatch sw = Stopwatch.StartNew();

            int len = html.Length;
            Out = new StringBuilder(len); // Pre-allocate space for the Markdown output

            inPre = false;                // Flag to indicate if we are inside a <pre> block
            char prevChr = '\0';          // Previous character for space handling
            bool repeatSpc = false;       // Flag to indicate if found a repeated space character

            // Main loop to process the HTML document
            for (int pos = 0; pos < len; pos++)
            {
                char c = html[pos];

                // Count new lines
                if (c == '\n')
                {
                    LineNum++;
                }

                // Skip \r characters
                else if (c == '\r')
                {
                    continue;
                }

                // Check for opening tag
                else if (c == '<')
                {
                    if (inPre)
                    {
                        ProcessTagInPre(html, len, ref pos);
                    }
                    else
                    {
                        ProcessTag(html, len, ref pos);
                    }
                    continue;
                }

                // Check for HTML entities
                else if (c == '&')
                {
                    // Check for HTML entities
                    if (DecodeHtmlEntity(html, ref pos, out char decoded))
                    {
                        if (markCodeBlock)
                        {
                            TextBuffer.Append(decoded);
                        }
                        else
                        {
                            TextBuffer.Append(EscapeMarkdownChars(decoded.ToString()));
                        }
                        continue;
                    }
                }

                if (inPre)
                {
                    // Preformatted text
                    if (c == '>')
                    {
                        ReportWarning($"Unexpected character `{c}` inside `<pre>` block.");
                    }
                    TextBuffer.Append(c);
                }
                else if (inTxt)
                {
                    // Check for double space or tab characters
                    if (c == ' ' || c == '\t' || c == '\n')
                    {
                        c = ' ';
                        repeatSpc = prevChr == ' ';
                    }

                    if (BuffSpc)
                    {
                        // Buffer space flag is set, add a space before the next text segment
                        TextBuffer.Append(' ');
                        BuffSpc = false;
                    }

                    // Handle repeated spaces
                    if (c == ' ')
                    {
                        if (!repeatSpc) { TextBuffer.Append(c); }
                    }
                    else
                    {
                        TextBuffer.Append(c);
                    }
                }

                prevChr = c;
            }

            CloseUnclosedTags(Out);

            // Write any buffered text to the output.
            // This is necessary to ensure that any text accumulated in the TextBuffer is written to the output.
            if (TextBuffer.Length > 0) { Out.Append(EscapeMarkdownChars(TextBuffer.ToString().Trim())); }

            if (!ignoreWarnings)
            {
                GenerateWarningsReport();
            }

            sw.Stop();
            double seconds = (double)sw.ElapsedTicks / Stopwatch.Frequency;
            Console.WriteLine($"HTML -> Markdown conv.: {seconds} sec.");

            return Out.ToString();
        }

        #endregion

        #region Helper methods

        /// <summary>
        /// Decodes HTML entities in the given HTML string.
        /// This method handles common HTML entities like &nbsp;, &lt;, &gt;, etc.
        /// </summary>
        /// <param name="html">The HTML string to process.</param>
        /// <param name="position">The current position in the HTML string.</param>
        /// <param name="decoded">The decoded character.</param>
        /// <returns>True if the entity was successfully decoded; otherwise, false.</returns>
        private bool DecodeHtmlEntity(string html, ref int position, out char decoded)
        {
            int endPos = html.IndexOf(';', position);
            int len = endPos - position + 1;

            if (len >= 4 && len <= 33)
            {
                string entity = html.Substring(position, len);
                switch (entity)
                {
                    case "&amp;":
                        decoded = '&';
                        break;
                    case "&lt;":
                        decoded = '<';
                        break;
                    case "&gt;":
                        decoded = '>';
                        break;
                    case "&nbsp;":
                        decoded = ' ';
                        break;
                    default:
                        decoded = WebUtility.HtmlDecode(entity)[0]; // Fallback
                        if (decoded == '&')
                        {
                            ReportWarning($"Unknown HTML entity: {entity.Replace("\n", "`CR`")}.");
                            return false;
                        }
                        break;
                }
                position = endPos;
                return true;
            }
            else
            {
                ReportWarning("Unknown HTML entity.");
                decoded = '\0';
                return false;
            }
        }

        /// <summary>
        /// Decodes HTML text by replacing HTML entities with their corresponding characters.
        /// This method processes the HTML string character by character,
        /// handling special characters and escaping Markdown characters.
        /// It ensures that the resulting Markdown text is properly formatted and does not contain any unescaped characters.
        /// </summary>
        /// <param name="html">The HTML string to process.</param>
        /// <returns>The decoded Markdown text.</returns>
        private StringBuilder DecodeHtmlText(string html)
        {
            int len = html.Length;
            StringBuilder sb = new StringBuilder();

            // Main loop to process the HTML document
            for (int pos = 0; pos < len; pos++)
            {
                char c = html[pos];

                // Check for HTML entities
                if (c == '&')
                {
                    // Check for HTML entities
                    if (DecodeHtmlEntity(html, ref pos, out char decoded))
                    {
                        c = decoded;
                    }
                }
                if (!IsTxtFormat && (c == '\\' || c == '*' || c == '_' || c == '#' || c == '$' || c == '`' || c == '\'' || c == '^' || c == '|' || c == '[' || c == ']' || c == '<' || c == '>' || c == '~'))
                {
                    sb.Append('\\');
                }
                sb.Append(c);
            }

            return sb;
        }

        /// <summary>
        /// Escapes special Markdown characters in the given text.
        /// This is necessary to prevent Markdown from interpreting them as formatting.
        /// </summary>
        private string EscapeMarkdownChars(string text)
        {
            StringBuilder sb = new StringBuilder(text.Length * 2); // Allocate more space for escaped characters
            foreach (char c in text)
            {
                if (!IsTxtFormat && (c == '\\' || c == '*' || c == '_' || c == '#' || c == '$' || c == '`' || c == '\'' || c == '^' || c == '|' || c == '[' || c == ']' || c == '<' || c == '>' || c == '~'))
                {
                    sb.Append('\\');
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Adds an empty line to the Markdown output.
        /// This method ensures that there are two newlines before the next content,
        /// which is the standard way to separate paragraphs in Markdown.
        /// </summary>
        /// <param name="outLen">Length of the output string.</param>
        private void EnsureEmptyLine(int outLen)
        {
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

        /// <summary>
        /// Adds a new row to the Markdown output.
        /// This method ensures that a new line is added before the next content,
        /// which is important for maintaining the structure of the Markdown document.
        /// If the last character is not a newline, it appends a newline character.
        /// </summary>
        private void EnsureNewline(char lastChar)
        {
            if (lastChar != '\n') { Out.Append('\n'); }
        }

        #endregion

        #region HTML tag processing

        private readonly StringBuilder FrontMatter = new StringBuilder();

        /// <summary>
        /// Buffer space flag.
        /// This flag is used to determine if a space should be added before the next text segment.
        /// </summary>
        private bool BuffSpc;

        /// <summary>
        /// Flags to track the current context in the HTML document.
        /// These flags help determine how to format the Markdown output based on the HTML structure.
        /// </summary>
        private bool skipHead, inHead, inTxt, inHeading, inList, inOrdList, inPre, inCode, inLink;

        /// <summary>
        /// Flags to track the state of blockquotes.
        /// These flags help manage how blockquotes are formatted in the Markdown output.
        /// </summary>
        private bool inBlockquote, contBlockquote = true, firstLineBlockquote = true;

        /// <summary>
        /// Flags to track the state of preformatted text and code blocks.
        /// These flags help manage how preformatted text and code blocks are formatted in the Markdown output.
        /// </summary>
        private bool preAddLine, codeAddLine;

        /// <summary>
        /// Holds the current hyperlink reference.
        /// </summary>
        private string href;

        /// <summary>
        /// Current level of nested lists.
        /// This is used to determine the indentation level for list items in the Markdown output.
        /// </summary>
        private int ListLevel;

        /// <summary>
        /// Item number for ordered lists.
        /// </summary>
        private int OlNum;

        /// <summary>
        /// Counters for the number of open tags.
        /// </summary>
        private int CntBld, CntItl, CntHl, CntDel;

        /// <summary>
        /// Processes an HTML tag at the current position in the HTML string.
        /// This method parses the tag, updates the Markdown output, and manages the context based on the tag type.
        /// It handles various HTML tags such as paragraphs, headings, links, lists, blockquotes,...
        /// </summary>
        /// <param name="html">The HTML string.</param>
        /// <param name="len">The length of the HTML string.</param>
        /// <param name="pos">The current position in the HTML string.</param>
        private void ProcessTag(string html, int len, ref int pos)
        {
            // Parses an HTML tag from the given position in the HTML string.
            string tagA = ParseTag(html, ref pos, out string tag, out int start);

            char tagChr1 = tag.Length >= 3 ? tag[1] : '\0';
            bool isParTag = false, isHeadingTag = false;
            bool improperClosed = false;

            // Head processing / Generate front matter
            if (!skipHead)
            {
                if (inHead)
                {
                    if (FrontMatter.Length == 0)
                    {
                        FrontMatter.Append("---\n");
                    }
                    if (tag == "</title>")
                    {
                        FrontMatter.Append($"title: {TextBuffer.ToString().Trim()}").Append('\n');
                        return;
                    }
                    else if (tag.StartsWith("<meta"))
                    {
                        string name = GetAttribute(tagA, "name");
                        string cont = GetAttribute(tagA, "content");
                        if (name.Length > 0)
                        {
                            FrontMatter.Append($"{name}: {DecodeHtmlText(cont.Trim())}");
                            FrontMatter.Append('\n');
                        }
                    }
                    else if (tag == "</head>")
                    {
                        inHead = false;
                        inTxt = false;
                        skipHead = true;
                        FrontMatter.Append("---\n");
                        Out.Append(FrontMatter);
                        TextBuffer.Clear();
                    }
                    return;
                }
                else if (tagChr1 == 'h' && tag.StartsWith("<head"))
                {
                    inHead = true;
                    inTxt = true;
                    return;
                }
            }

            // Preformatted text
            if (tagChr1 == 'p' && tag == "<pre>")
            {
                inPre = true;
                preAddLine = pos + 1 < len && html[pos + 1] != '\n';
            }

            // Paragraph tags
            else if (tagChr1 == 'p' && (tag[2] == '>' || tag[2] == ' '))
            {
                isParTag = true;
                if ((inTxt && !inBlockquote) || inHeading)
                {
                    improperClosed = true;
                    ReportWarning("Improperly closed paragraph/heading tag", inPrevLine: true);
                }
            }

            // Heading tags
            else if (tagChr1 == 'h' && char.IsDigit(tag[2]))
            {
                isHeadingTag = true;
                if (inTxt || inHeading)
                {
                    improperClosed = true;
                    ReportWarning("Improperly closed paragraph/heading tag", inPrevLine: true);
                }
            }

            // Write the accumulated text to the Markdown output
            string content = "";
            if (tag.StartsWith("</") || improperClosed)
            {
                if (TextBuffer.Length > 0)
                {
                    if (TextBuffer[TextBuffer.Length - 1] == ' ' && !improperClosed)
                    {
                        BuffSpc = true;
                    }
                    if (inPre)
                    {
                        if (inCode)
                        {
                            if (codeAddLine) { Out.Append("\n"); }
                        }
                        else
                        {
                            if (MarkCodeBlock) { Out.Append(" text"); }
                            if (preAddLine) { Out.Append("\n"); }
                        }
                        content = TextBuffer.ToString();
                    }
                    else
                    {
                        content = EscapeMarkdownChars(TextBuffer.ToString().TrimEnd());
                    }
                }
            }
            else
            {
                if (!inPre)
                {
                    // Skip spaces and newlines after start of tag to avoid unwanted whitespace in Markdown output.
                    while (pos + 1 < len && (html[pos + 1] == ' ' || html[pos + 1] == '\n'))
                    {
                        if (html[pos + 1] == '\n')
                        {
                            LineNum++; // Increment line number for each newline character
                        }
                        pos++;
                    }
                }
                content = EscapeMarkdownChars(TextBuffer.ToString());
                BuffSpc = false;
            }
            if (content.Length > 0)
            {
                if (inBlockquote && !contBlockquote)
                {
                    Out.Append("> ");
                    contBlockquote = true;
                    firstLineBlockquote = false;
                }
                if (inLink)
                {
                    Out.Append($"[{content}]({href})");
                }
                else
                {
                    Out.Append(content);
                }
            }
            TextBuffer.Clear();

            int outLen = Out.Length;
            char lastChar = outLen > 0 ? Out[outLen - 1] : '\0';

            // Paragraph handling
            if (isParTag)
            {
                inTxt = true;
                if (inBlockquote && !firstLineBlockquote)
                {
                    Out.Append("\n>\n");
                }
                else
                {
                    EnsureEmptyLine(outLen);
                }
            }

            // Heading handling
            else if (isHeadingTag)
            {
                inTxt = true;
                inHeading = true;
                int level = tag[2] - '0';
                EnsureEmptyLine(outLen);
                if (!IsTxtFormat)
                {
                    Out.Append($"{new string('#', level)} ");
                }
                //string id = GetAttribute(tag, "id");
                //if (!string.IsNullOrEmpty(id))
                //{
                //    sb.Append($"<a id=\"{id}\"></a> ");
                //}
            }

            // Links
            else if (tagChr1 == 'a' && tag.StartsWith("<a "))
            {
                inLink = true;
                href = GetAttribute(tagA, "href");
            }

            // Span
            else if (tagChr1 == 's' && tag.StartsWith("<span"))
            {
                // Ignore span tags for now
            }

            // Div
            else if (tagChr1 == 'd' && tag.StartsWith("<div"))
            {
                // Ignore div tags for now
            }

            // Tables
            else if (tagChr1 == 't' && tag.StartsWith("<table"))
            {
                pos = start;
                ParseTable(html, ref pos);
            }

            // Preformatted text
            else if (inPre && tag == "<pre>")
            {
                EnsureEmptyLine(Out.Length);
                if (MarkCodeBlock) { Out.Append("```"); }
            }

            // Code blocks
            else if (tagChr1 == 'c' && tag.StartsWith("<code"))
            {
                inCode = true;
                string atr = GetAttribute(tagA, "class");
                codeAddLine = pos + 1 < len && html[pos + 1] != '\n';
                if (MarkCodeBlock) { Out.Append(atr.Length > 0 ? " " + atr : ""); }
            }

            // Images
            else if (tagChr1 == 'i' && tag.StartsWith("<img"))
            {
                string src = GetAttribute(tagA, "src");
                string alt = GetAttribute(tagA, "alt");
                Out.Append($"![{alt}]({src})");
            }

            // Task lists with checkboxes
            else if (tagChr1 == 'i' && inList && !inOrdList && tag.StartsWith("<input"))
            {
                string atr = GetAttribute(tagA, "type");
                if (atr == "checkbox")
                {
                    if (tag.Contains("checked"))
                    {
                        Out.Append("[x] ");
                    }
                    else
                    {
                        Out.Append("[ ] ");
                    }
                }
            }

            // Ignore doctype, and html/body tags
            else if (tagChr1 != '!' && (tagChr1 != 'h' || !tag.StartsWith("<html")) && (tagChr1 != 'b' || !tag.StartsWith("<body")))
            {
                // Handle different HTML tags
                switch (tag)
                {
                    // Paragraphs
                    case "</p>":
                        CloseUnclosedTags(Out);
                        inTxt = false;
                        BuffSpc = false;
                        if (inBlockquote)
                        {
                            contBlockquote = false;
                        }
                        else
                        {
                            Out.Append("\n");
                        }
                        break;

                    // Basic text formatting
                    case "<strong>":
                    case "<b>":
                        CntBld++;
                        Out.Append("**");
                        break;
                    case "</strong>":
                    case "</b>":
                        if (CntBld > 0)
                        {
                            CntBld--;
                            Out.Append("**");
                        }
                        break;

                    case "<em>":
                    case "<i>":
                        CntItl++;
                        Out.Append("*");
                        break;
                    case "</em>":
                    case "</i>":
                        if (CntItl > 0)
                        {
                            CntItl--;
                            Out.Append("*");
                        }
                        break;

                    case "<del>":
                        CntDel++;
                        Out.Append("~~");
                        break;
                    case "</del>":
                        if (CntDel > 0)
                        {
                            CntDel--;
                            Out.Append("~~");
                        }
                        break;

                    case "<mark>":
                        CntHl++;
                        Out.Append("==");
                        break;
                    case "</mark>":
                        if (CntHl > 0)
                        {
                            CntHl--;
                            Out.Append("==");
                        }
                        break;

                    // Ordered and unordered lists
                    case "<ul>":
                    case "<ol>":
                        inOrdList = tag == "<ol>";
                        if (inOrdList && ListLevel == 0)
                        {
                            OlNum = 0;
                        }
                        if (!inList)
                        {
                            EnsureEmptyLine(outLen);
                        }
                        inList = true;
                        ListLevel++;
                        break;
                    case "</ul>":
                    case "</ol>":
                        ListLevel--;
                        if (ListLevel == 0)
                        {
                            OlNum = 0;
                            inList = false;
                        }
                        EnsureNewline(lastChar);
                        break;
                    case "<li>":
                        inTxt = true;
                        EnsureNewline(lastChar);
                        if (inOrdList)
                        {
                            OlNum++;
                            Out.Append(inList ? $"{new string(' ', (ListLevel - 1) * 2)}{OlNum}. " : "- ");
                        }
                        else
                        {
                            Out.Append(inList ? $"{new string(' ', (ListLevel - 1) * 2)}- " : "- ");
                        }
                        break;
                    case "</li>":
                        inTxt = false;
                        BuffSpc = false;
                        break;

                    // Headings
                    case "</h1>":
                    case "</h2>":
                    case "</h3>":
                        inTxt = false;
                        inHeading = false;
                        BuffSpc = false;
                        if (IsTxtFormat)
                        {
                            if (tag == "</h1>")
                            {
                                Out.Append("\n" + new string('=', content.Length));
                            }
                            else
                            {
                                Out.Append("\n" + new string('-', content.Length));
                            }
                        }
                        Out.Append("\n\n");
                        break;

                    // Links
                    case "</a>":
                        inLink = false;
                        break;

                    case "</pre>":
                        inPre = false;
                        inCode = false;
                        EnsureNewline(lastChar);
                        if (MarkCodeBlock) { Out.Append("```\n\n"); }
                        break;

                    // Code blocks
                    case "</code>":
                        inCode = false;
                        EnsureNewline(lastChar);
                        break;

                    // Blockquotes
                    case "<blockquote>":
                        inTxt = true;
                        inBlockquote = true;
                        firstLineBlockquote = true;
                        contBlockquote = false;
                        EnsureNewline(lastChar);
                        break;
                    case "</blockquote>":
                        inTxt = false;
                        inBlockquote = false;
                        BuffSpc = false;
                        Out.Append('\n');
                        break;

                    // Line breaks
                    case "<br>":
                    case "<br/>":
                    case "<br />":
                        if (inTxt)
                        {
                            if (lastChar != ' ')
                            {
                                if (inList)
                                {
                                    Out.Append("\n  ");
                                }
                                else
                                {
                                    Out.Append("  \n");
                                }
                            }
                        }
                        else
                        {
                            EnsureEmptyLine(outLen);
                            if (IsTxtFormat) { Out.Append('\n'); }
                            else { Out.Append("\\"); }
                        }
                        break;

                    // Horizontal rule
                    case "<hr>":
                    case "<hr/>":
                    case "<hr />":
                        EnsureEmptyLine(outLen);
                        Out.Append("---\n");
                        break;

                    // Span
                    case "</span>":

                    // Divs
                    case "</div>":
                        break;

                    // Subscripts and superscripts
                    case "<sub>":
                    case "</sub>":
                        Out.Append("~");
                        break;
                    case "<sup>":
                    case "</sup>":
                        Out.Append("^");
                        break;

                    // Ignore other tags
                    case "</body>":
                    case "</html>":
                        CloseUnclosedTags(Out);
                        break;

                    // Unknown or unsupported tags
                    default:
                        inTxt = !tag.StartsWith("</");
                        break;
                }
            }
        }

        /// <summary>
        /// Processes an HTML tag within a <pre> block.
        /// </summary>
        /// <param name="html">The HTML string.</param>
        /// <param name="len">The length of the HTML string.</param>
        /// <param name="pos">The current position in the HTML string.</param>
        private void ProcessTagInPre(string html, int len, ref int pos)
        {
            string tagA = ParseTag(html, ref pos, out string tag, out int start);

            switch (tag)
            {
                case "</pre>":
                    CloseUnclosedTags(Out);
                    pos = start;
                    ProcessTag(html, len, ref pos);
                    break;


                // Ignore basic formatting tags inside <pre> block
                case "<b>":
                case "<strong>":
                case "</b>":
                case "</strong>":
                case "<i>":
                case "<em>":
                case "</i>":
                case "</em>":
                case "<del>":
                case "</del>":
                case "<mark>":
                case "</mark>":
                    break;

                default:
                    if (tag.StartsWith("<a href"))
                    {
                        string href = GetAttribute(tagA, "href");
                        int i = html.IndexOf("</a>", pos);
                        if (i > -1)
                        {
                            string name = html.Substring(pos + 1, i - pos - 1);
                            TextBuffer.Append($"[{name}]({href})");
                        }
                    }
                    else if (tag.StartsWith("<a id"))
                    {
                        TextBuffer.Append($"{tagA}</a>");
                    }
                    else if (tag.StartsWith("<code"))
                    {
                        inCode = true;
                        string atr = GetAttribute(tagA, "class");
                        codeAddLine = pos + 1 < len && html[pos + 1] != '\n';
                        if (MarkCodeBlock) { Out.Append(atr.Length > 0 ? " " + atr : ""); }
                    }
                    else if (tag == "</a>" || tag.StartsWith("<!--") || tag == "</code>" || tag.StartsWith("<span") || tag == "</span>")
                    {
                        //
                    }
                    else
                    {
                        TextBuffer.Append('<');
                        ReportWarning("Unexpected character `<` inside `<pre>` block.");
                        pos = start;
                    }
                    break;
            }
        }

        /// <summary>
        /// Parses an HTML tag from the given position in the HTML string.
        /// This method extracts the tag name and attributes, and updates the position in the HTML string.
        /// </summary>
        /// <param name="html">HTML string.</param>
        /// <param name="p">Current position in the HTML string.</param>
        /// <param name="tagLower">Lowercase tag name.</param>
        /// <param name="start">Start position of the tag.</param>
        private string ParseTag(string html, ref int p, out string tagLower, out int start)
        {
            start = p;
            while (p < html.Length && html[p] != '>')
            {
                p++;
            }
            string tag = html.Substring(start, p - start + 1);
            tagLower = tag.ToLower();
            return tag;
        }

        /// <summary>
        /// Extracts the value of a specific attribute from an HTML tag.
        /// </summary>
        private static string GetAttribute(string tag, string attrName)
        {
            int start = tag.IndexOf(attrName + "=\"", StringComparison.OrdinalIgnoreCase);
            if (start == -1) { return ""; }

            start += attrName.Length + 2;
            int end = tag.IndexOf('"', start);
            return end > start ? tag.Substring(start, end - start) : "";
        }

        /// <summary>
        /// Closes any unclosed tags in the Markdown output, and reports warnings for each unclosed tag.
        /// This method ensures that all opened tags are properly closed before the end of the document.
        /// </summary>
        private void CloseUnclosedTags(StringBuilder sb)
        {
            while (CntItl > 0)
            {
                sb.Append("*");
                ReportWarning("Unclosed italic tag", before: true);
                CntItl--;
            }
            while (CntBld > 0)
            {
                sb.Append("**");
                ReportWarning("Unclosed bold tag", before: true);
                CntBld--;
            }
            while (CntHl > 0)
            {
                sb.Append("==");
                ReportWarning("Unclosed mark tag", before: true);
                CntHl--;
            }
            while (CntDel > 0)
            {
                sb.Append("~~");
                ReportWarning("Unclosed del tag", before: true);
                CntDel--;
            }
        }

        #endregion

        #region Table processing

        /// <summary>
        /// Represents a cell in an HTML table.
        /// </summary>
        private class TableCell
        {
            public StringBuilder Content { get; set; }
            public int ColSpan { get; set; }
            public int RowSpan { get; set; }
            public bool IsHeader { get; set; }
            public TextAlignment Alignment { get; set; }

            public TableCell()
            {
                Content = new StringBuilder();
                ColSpan = 1;
                RowSpan = 1;
            }
        }

        private enum TextAlignment { Left, Center, Right }

        /// <summary>
        /// Parses an HTML table and converts it to Markdown format.
        /// This method processes the table structure, including rows and cells,
        /// and generates the corresponding Markdown representation.
        /// It handles table headers, cell alignment, and spans (colspan/rowspan).
        /// </summary>
        private void ParseTable(string html, ref int pos)
        {
            List<List<TableCell>> rows = new List<List<TableCell>>();
            List<TableCell> row = new List<TableCell>();
            TableCell cell = null;
            bool inTable = true;
            pos--;
            LineNum--;

            while (pos < html.Length && inTable)
            {
                char c = html[++pos];

                if (c == '\n')
                {
                    LineNum++; // Increment line number for each newline character
                }
                else if (c == '<')
                {
                    ParseTag(html, ref pos, out string tag, out _);

                    if (tag.StartsWith("<table"))
                    {
                        // Ignore nested tables
                        if (rows.Count > 0) { continue; }
                    }
                    else if (tag == "</table>")
                    {
                        inTable = false;
                        EnsureEmptyLine(Out.Length);
                    }
                    else if (tag.StartsWith("<tr"))
                    {
                        if (row.Count > 0)
                        {
                            rows.Add(row);
                        }
                        row = new List<TableCell>();
                    }
                    else if (tag.StartsWith("<th"))
                    {
                        cell = new TableCell { IsHeader = true };
                        ParseCellAttributes(tag, cell);
                    }
                    else if (tag.StartsWith("<td"))
                    {
                        cell = new TableCell();
                        ParseCellAttributes(tag, cell);
                    }
                    else if (tag.StartsWith("</th") || tag.StartsWith("</td"))
                    {
                        if (cell != null)
                        {
                            row.Add(cell);
                            cell = null;
                        }
                    }
                    // Ignore all other tags inside cells
                }
                else if (cell != null && !char.IsWhiteSpace(c))
                {
                    cell.Content.Append(c);
                }
            }

            if (row.Count > 0) { rows.Add(row); }

            RenderTable(rows);
        }

        /// <summary>
        /// Parses the attributes of a table cell from an HTML tag.
        /// </summary>
        private void ParseCellAttributes(string tag, TableCell cell)
        {
            // Handle colspan/rowspan
            cell.ColSpan = GetSpanAttribute(tag, "colspan");
            cell.RowSpan = GetSpanAttribute(tag, "rowspan");

            // Alignment detection
            if (tag.IndexOf("text-align:center", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                cell.Alignment = TextAlignment.Center;
            }
            else if (tag.IndexOf("text-align:right", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                cell.Alignment = TextAlignment.Right;
            }
        }

        /// <summary>
        /// Gets the value of a span attribute (colspan or rowspan) from an HTML table cell tag.
        /// </summary>
        private int GetSpanAttribute(string tag, string attr)
        {
            int start = tag.IndexOf(attr + "=\"", StringComparison.OrdinalIgnoreCase);
            if (start < 0) { return 1; }

            start += attr.Length + 2;
            int end = tag.IndexOf('"', start);
            if (end < 0) { return 1; }

            return int.TryParse(tag.Substring(start, end - start), out int value) ? Math.Max(1, value) : 1;
        }

        /// <summary>
        /// Renders a Markdown table from the parsed rows.
        /// This method formats the table with headers, separators, and data rows,
        /// ensuring proper alignment based on the cell attributes.
        /// </summary>
        private void RenderTable(List<List<TableCell>> rows)
        {
            if (rows.Count == 0) { return; }

            // Header
            Out.Append("|");
            foreach (TableCell cell in rows[0])
            {
                Out.Append(" ").Append(DecodeHtmlText(cell.Content.ToString())).Append(" |");
            }
            Out.Append('\n');

            // Separator
            Out.Append("|");
            foreach (TableCell cell in rows[0])
            {
                Out.Append(cell.Alignment == TextAlignment.Center
                    ? " :---: |" :
                    cell.Alignment == TextAlignment.Right
                        ? " ---: |" : " --- |");
            }
            Out.Append('\n');

            // Data rows
            for (int i = 1; i < rows.Count; i++)
            {
                Out.Append("|");
                foreach (TableCell cell in rows[i])
                {
                    Out.Append(" ").Append(DecodeHtmlText(cell.Content.ToString())).Append(" |");
                }
                Out.Append('\n');
            }
        }

        #endregion

        #region Warnings processing

        /// <summary>
        /// List of warnings encountered during HTML parsing.
        /// This list is used to collect warnings about potential issues in the HTML text,
        /// such as unclosed tags or incorrect formatting.
        /// </summary>
        private readonly List<string> Warnings = new List<string>();

        /// <summary>
        /// Reports a warning encountered during HTML parsing.
        /// </summary>
        /// <param name="desc">Description of the warning.</param>
        private void ReportWarning(string desc, bool before = false, bool inPrevLine = false)
        {
            Warnings.Add((before ? "Line <= " : "Line ") + (inPrevLine ? LineNum - 1 : LineNum) + ": " + desc);
        }

        /// <summary>
        /// Generates a report of any warnings encountered during HTML parsing.
        /// </summary>
        private void GenerateWarningsReport()
        {
            // Generate a report of any warnings
            if (Warnings.Count > 0)
            {
                EnsureEmptyLine(Out.Length);
                Out.Append(new string('-', 52) + "\n");
                EnsureEmptyLine(Out.Length);
                Out.Append("## ⚠️ WARNINGS\n\n");
                foreach (string desc in Warnings)
                {
                    Out.Append($"- {desc}\n");
                }
            }
        }

        #endregion
    }
}
