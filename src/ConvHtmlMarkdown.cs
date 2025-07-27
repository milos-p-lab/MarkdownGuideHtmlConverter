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
    /// <version>2.0.0</version>
    /// <date>2025-07-25</date>
    /// <author>Miloš Perunović</author>
    public class ConvHtmlMarkdown
    {
        #region Main methods for converting HTML to Markdown

        /// <summary>
        /// Converts HTML document to Markdown.
        /// </summary>
        /// <param name="html">The HTML document as a string.</param>
        /// <returns>Markdown representation of the HTML document</returns>
        public static string Convert(string html)
        {
            return new ConvHtmlMarkdown().ToMarkdownBody(html);
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
        /// Converts HTML document to Markdown.
        /// </summary>
        /// <param name="html">The HTML document as a string.</param>
        /// <returns>Markdown representation of the HTML document</returns>
        private string ToMarkdownBody(string html)
        {
            Stopwatch sw = Stopwatch.StartNew();

            int len = html.Length;
            Out = new StringBuilder(len); // Pre-allocate space for the Markdown output

            inPre = false; // Flag to indicate if we are inside a <pre> block
            char prevChr = '\0'; // Previous character for space handling
            bool repeatSpc = false; // Flag to indicate if found a repeated space character

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
                    bool tagOk = true;
                    if (inPre)
                    {
                        // Check for unexpected characters inside <pre> block
                        string t = ParseTag(html, ref pos, out _, out int start);
                        pos = start;
                        t = t.Replace("<", "").Replace(">", "").Replace("/", "");
                        if (
                            !t.StartsWith("pre") && !t.StartsWith("code") && !t.StartsWith("span") &&
                            t != "b" && t != "i" && t != "u" && t != "strong" && t != "em"
                            )
                        {
                            tagOk = false;
                            ReportWarning($"Unexpected character `{c}` inside `<pre>` block.");
                        }
                    }
                    if (tagOk)
                    {
                        ProcessTag(html, len, ref pos);
                        continue;
                    }
                }

                // Check for double space or tab characters
                else if (c == ' ' || c == '\t')
                {
                    c = ' ';
                    repeatSpc = prevChr == ' ';
                }

                // Check for HTML entities
                else if (c == '&')
                {
                    // Check for HTML entities
                    if (DecodeHtmlEntity(html, ref pos, out char decoded))
                    {
                        TextBuffer.Append(decoded);
                        continue;
                    }
                }

                prevChr = c;

                if (inPre)
                {
                    // Preformatted text
                    if (c == '>')
                    {
                        ReportWarning($"Unexpected character `{c}` inside `<pre>` block.");
                    }
                    TextBuffer.Append(html[pos]);
                }
                else if (inTxt && c != '\n')
                {
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
            }

            // Process any remaining text in the buffer
            if (TextBuffer.Length > 0)
            {
                Out.Append(TextBuffer.ToString().Trim());
            }

            GenerateWarningsReport();

            sw.Stop();
            double seconds = (double)sw.ElapsedTicks / Stopwatch.Frequency;
            Console.WriteLine($"HTML -> Markdown conv.: {seconds} sec.");

            return Out.ToString();
        }

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
                if (c == '\\' || c == '*' || c == '_' || c == '#' || c == '$' || c == '`' || c == '\'' || c == '^' || c == '|' || c == '[' || c == ']' || c == '<' || c == '>' || c == '~')
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
        private static string EscapeMarkdownChars(string text)
        {
            StringBuilder sb = new StringBuilder(text.Length * 2); // Allocate more space for escaped characters
            foreach (char c in text)
            {
                if (c == '\\' || c == '*' || c == '_' || c == '#' || c == '$' || c == '`' || c == '\'' || c == '^' || c == '|' || c == '[' || c == ']' || c == '<' || c == '>' || c == '~')
                {
                    sb.Append('\\');
                }
                sb.Append(c);
            }
            return sb.ToString();
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
        /// Item number for ordered lists.
        /// </summary>
        private int olNum;

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
        /// Processes an HTML tag at the current position in the HTML string.
        /// This method parses the tag, updates the Markdown output, and manages the context based on the tag type.
        /// It handles various HTML tags such as paragraphs, headings, links, lists, blockquotes,...
        /// </summary>
        private void ProcessTag(string html, int len, ref int pos)
        {
            // Parses an HTML tag from the given position in the HTML string.
            string tag = ParseTag(html, ref pos, out string tagL, out int start);

            char tagChr = tag.Length >= 3 ? tagL[1] : '\0';
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
                    if (tagL == "</title>")
                    {
                        FrontMatter.Append($"title: {TextBuffer.ToString().Trim()}").Append('\n');
                        return;
                    }
                    else if (tagL.StartsWith("<meta"))
                    {
                        string name = GetAttribute(tag, "name");
                        string cont = GetAttribute(tag, "content");
                        if (name.Length > 0)
                        {
                            FrontMatter.Append($"{name}: {DecodeHtmlText(cont.Trim())}");
                            FrontMatter.Append('\n');
                        }
                    }
                    else if (tagL == "</head>")
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
                else if (tagChr == 'h' && tagL.StartsWith("<head"))
                {
                    inHead = true;
                    inTxt = true;
                    return;
                }
            }

            // Preformatted text
            if (tagChr == 'p' && tagL == "<pre>")
            {
                inPre = true;
                preAddLine = pos + 1 < len && html[pos + 1] != '\n';
            }

            // Paragraph tags
            else if (tagChr == 'p' && (tag[2] == '>' || tag[2] == ' '))
            {
                isParTag = true;
                if ((inTxt && !inBlockquote) || inHeading)
                {
                    improperClosed = true;
                    ReportWarning("Improperly closed paragraph/heading tag", inPrevLine: true);
                }
            }

            // Heading tags
            else if (tagChr == 'h' && char.IsDigit(tag[2]))
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
                            Out.Append(" text");
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
                    AddEmptyLine(outLen);
                }
            }

            // Heading handling
            else if (isHeadingTag)
            {
                inTxt = true;
                inHeading = true;
                int level = tag[2] - '0';
                AddEmptyLine(outLen);
                Out.Append($"{new string('#', level)} ");

                //string id = GetAttribute(tag, "id");
                //if (!string.IsNullOrEmpty(id))
                //{
                //    sb.Append($"<a id=\"{id}\"></a> ");
                //}

            }

            // Links
            else if (tagChr == 'a' && tagL.StartsWith("<a "))
            {
                inLink = true;
                href = GetAttribute(tag, "href");
            }

            // Span
            else if (tagChr == 's' && tagL.StartsWith("<span"))
            {
                // Ignore span tags for now
            }

            // Tables
            else if (tagChr == 't' && tagL.StartsWith("<table"))
            {
                pos = start;
                ParseTable(html, ref pos);
            }

            // Preformatted text
            else if (inPre && tagL == "<pre>")
            {
                AddEmptyLine(Out.Length);
                Out.Append("```");
            }

            // Code blocks
            else if (tagChr == 'c' && tagL.StartsWith("<code"))
            {
                inCode = true;
                string atr = GetAttribute(tag, "class");
                codeAddLine = pos + 1 < len && html[pos + 1] != '\n';
                Out.Append(atr.Length > 0 ? " " + atr : "");
            }

            // Images
            else if (tagChr == 'i' && tagL.StartsWith("<img"))
            {
                string src = GetAttribute(tag, "src");
                string alt = GetAttribute(tag, "alt");
                Out.Append($"![{alt}]({src})");
            }

            // Task lists with checkboxes
            else if (tagChr == 'i' && inList && !inOrdList && tagL.StartsWith("<input"))
            {
                string atr = GetAttribute(tag, "type");
                if (atr == "checkbox")
                {
                    if (tagL.Contains("checked"))
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
            else if (tagChr != '!' && (tagChr != 'h' || !tagL.StartsWith("<html")) && (tagChr != 'b' || !tagL.StartsWith("<body")))
            {
                // Handle different HTML tags
                switch (tagL)
                {
                    // Paragraphs
                    case "</p>":
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
                    case "</strong>":
                    case "</b>":
                        Out.Append("**");
                        break;
                    case "<em>":
                    case "<i>":
                    case "</em>":
                    case "</i>":
                        Out.Append("*");
                        break;
                    case "<del>":
                    case "</del>":
                        Out.Append("~~");
                        break;
                    case "<mark>":
                    case "</mark>":
                        Out.Append("==");
                        break;

                    // Ordered and unordered lists
                    case "<ul>":
                    case "<ol>":
                        inOrdList = tag == "<ol>";
                        if (inOrdList && ListLevel == 0)
                        {
                            olNum = 0;
                        }
                        if (!inList)
                        {
                            AddEmptyLine(outLen);
                        }
                        inList = true;
                        ListLevel++;
                        break;
                    case "</ul>":
                    case "</ol>":
                        ListLevel--;
                        if (ListLevel == 0)
                        {
                            olNum = 0;
                            inList = false;
                        }
                        AddNewRow(lastChar);
                        break;
                    case "<li>":
                        inTxt = true;
                        AddNewRow(lastChar);
                        if (inOrdList)
                        {
                            olNum++;
                            Out.Append(inList ? $"{new string(' ', (ListLevel - 1) * 2)}{olNum}. " : "- ");
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
                        Out.Append("\n\n");
                        break;

                    // Links
                    case "</a>":
                        inLink = false;
                        break;

                    case "</pre>":
                        inPre = false;
                        AddNewRow(lastChar);
                        Out.Append("```\n\n");
                        break;

                    // Code blocks
                    case "</code>":
                        inCode = false;
                        AddNewRow(lastChar);
                        break;

                    // Blockquotes
                    case "<blockquote>":
                        inTxt = true;
                        inBlockquote = true;
                        firstLineBlockquote = true;
                        contBlockquote = false;
                        AddNewRow(lastChar);
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
                            if (lastChar != ' ') { Out.Append("  \n"); }
                        }
                        else
                        {
                            AddEmptyLine(outLen);
                            Out.Append("\\");
                        }
                        break;

                    // Horizontal rule
                    case "<hr>":
                    case "<hr/>":
                    case "<hr />":
                        AddEmptyLine(outLen);
                        Out.Append("---\n");
                        break;

                    // Span
                    case "</span>":
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
                        break;

                    // Unknown or unsupported tags
                    default:
                        inTxt = !tag.StartsWith("</");
                        break;
                }
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
        /// Adds an empty line to the Markdown output.
        /// This method ensures that there are two newlines before the next content,
        /// which is the standard way to separate paragraphs in Markdown.
        /// </summary>
        /// <param name="outLen">Length of the output string.</param>
        private void AddEmptyLine(int outLen)
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
        private void AddNewRow(char lastChar)
        {
            if (lastChar != '\n') { Out.Append('\n'); }
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
                        AddEmptyLine(Out.Length);
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
        private void ReportWarning(string desc, bool inPrevLine = false)
        {
            Warnings.Add($"Line {(inPrevLine ? LineNum - 1 : LineNum)}: {desc}");
        }

        /// <summary>
        /// Generates a report of any warnings encountered during HTML parsing.
        /// </summary>
        private void GenerateWarningsReport()
        {
            // Generate a report of any warnings
            if (Warnings.Count > 0)
            {
                AddEmptyLine(Out.Length);
                Out.Append(new string('-', 52) + "\n");
                AddEmptyLine(Out.Length);
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
