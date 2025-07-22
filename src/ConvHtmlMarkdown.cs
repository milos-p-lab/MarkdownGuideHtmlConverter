using System;
using System.Diagnostics;
using System.Text;

namespace m.format.conv
{
    /// <summary>
    /// Converts HTML to Markdown.
    /// </summary>
    /// <version>1.4.1</version>
    /// <date>2025-07-22</date>
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

            for (int p = 0; p < len; p++)
            {
                char c = html[p];

                // Check for opening tag
                if (c == '<')
                {
                    ProcessTag(html, len, ref p);
                }
                else if (inPre)
                {
                    // Ignore
                }
                else if (c == '\n' || c == '\r' || (!inTxt && c == ' '))
                {
                    // Ignore
                }
                else
                {
                    TextBuffer.Append(c);
                }
            }

            if (TextBuffer.Length > 0)
            {
                Out.Append(TextBuffer.ToString().Trim());
            }

            sw.Stop();
            double seconds = (double)sw.ElapsedTicks / Stopwatch.Frequency;
            Console.WriteLine($"HTML -> Markdown conv.: {seconds} sec.");

            return Out.ToString();
        }

        #endregion

        #region HTML tag processing

        /// <summary>
        /// Flags to track the current context in the HTML document.
        /// These flags help determine how to format the Markdown output based on the HTML structure.
        /// </summary>
        private bool inTxt, inList, inPre, inLink;

        /// <summary>
        /// Flags to track the state of blockquotes.
        /// These flags help manage how blockquotes are formatted in the Markdown output.
        /// </summary>
        private bool inBlockquote, contBlockquote = true, firstLineBlockquote = true;

        private string href;

        /// <summary>
        /// Current level of nested lists.
        /// This is used to determine the indentation level for list items in the Markdown output.
        /// </summary>
        private int ListLevel;

        private void ProcessTag(string html, int len, ref int p)
        {
            // Parses an HTML tag from the given position in the HTML string.
            int start = p;
            while (p < html.Length && html[p] != '>')
            {
                p++;
            }
            string tag = html.Substring(start, p - start + 1);
            string tagLow = tag.ToLower();
            int outLen = Out.Length;

            char firstTagChr = tag.Length >= 3 ? tagLow[1] : '\0';

            // Write the accumulated text to the Markdown output
            string content;
            if (tag.StartsWith("</"))
            {
                content = EscapeMarkdownChars(TextBuffer.ToString().TrimEnd());
            }
            else
            {
                while (p + 1 < len && (html[p + 1] == ' ' || html[p + 1] == '\n'))
                {
                    p++;
                }
                content = EscapeMarkdownChars(TextBuffer.ToString());
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

            // Paragraph handling
            if (firstTagChr == 'p' && (tag[2] == '>' || tag[2] == ' '))
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
            else if (firstTagChr == 'h' && char.IsDigit(tag[2]))
            {
                inTxt = true;
                int level = tag[2] - '0';
                AddEmptyLine(outLen);
                Out.Append($"{new string('#', level)} ");

                //string id = GetAttribute(tag, "id");
                //if (!string.IsNullOrEmpty(id))
                //{
                //    sb.Append($"<a id=\"{id}\"></a> ");
                //}

            }

            // Link handling
            else if (firstTagChr == 'a')
            {
                inLink = true;
                href = GetAttribute(tag, "href");
            }
            else
            {
                char outLastChr = outLen > 0 ? Out[outLen - 1] : '\0';

                // Handle different HTML tags
                switch (tagLow)
                {
                    // Paragraphs
                    case "</p>":
                        inTxt = false;
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

                    // Ordered and unordered lists
                    case "<ul>":
                    case "<ol>":
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
                        if (ListLevel == 0) { inList = false; }
                        if (outLastChr != '\n') { Out.Append('\n'); }

                        break;
                    case "<li>":
                        inTxt = true;
                        if (outLastChr != '\n') { Out.Append('\n'); }
                        Out.Append(inList ? $"{new string(' ', (ListLevel - 1) * 2)}- " : "- ");
                        break;
                    case "</li>":
                        inTxt = false;
                        break;

                    // Headings
                    case "</h1>":
                    case "</h2>":
                    case "</h3>":
                        inTxt = false;
                        Out.Append("\n\n"); break;

                    case "</a>":
                        inLink = false;
                        break;

                    // Code/preformatted
                    case "<pre>":
                        inPre = true;
                        Out.Append("\n```\n");
                        break;
                    case "</pre>":
                        inPre = false;
                        Out.Append("\n```\n");
                        break;
                    case "<code>":
                    case "</code>":
                        Out.Append(inPre ? "" : "`");
                        break;

                    // Blockquotes
                    case "<blockquote>":
                        inTxt = true;
                        inBlockquote = true;
                        firstLineBlockquote = true;
                        contBlockquote = false;
                        if (outLastChr != '\n') { Out.Append('\n'); }
                        break;
                    case "</blockquote>":
                        inTxt = false;
                        inBlockquote = false;
                        Out.Append('\n');
                        break;

                    // Images
                    case "<img ":
                        string src = GetAttribute(tag, "src");
                        string alt = GetAttribute(tag, "alt");
                        Out.Append($"![{alt}]({src})");
                        break;

                    // Line breaks
                    case "<br>":
                    case "<br/>":
                    case "<br />":
                        if (inTxt) { Out.Append('\\'); }
                        Out.Append("\n");
                        break;

                    // Horizontal rule
                    case "<hr>":
                    case "<hr/>":
                    case "<hr />":
                        Out.Append("\n---\n");
                        break;

                    // Unknown or unsupported tags
                    default:
                        inTxt = !tag.StartsWith("</");
                        break;
                }
            }
        }

        /// <summary>
        /// Extracts the value of a specific attribute from an HTML tag.
        /// </summary>
        private static string GetAttribute(string tag, string attrName)
        {
            int start = tag.IndexOf(attrName + "=\"");
            if (start == -1) { return ""; }

            start += attrName.Length + 2;
            int end = tag.IndexOf('"', start);
            return end > start ? tag.Substring(start, end - start) : "";
        }

        private void AddEmptyLine(int outLen)
        {
            string last2 = outLen > 2 ? Out.ToString(outLen - 2, 2) : "\n\n";
            if (last2.EndsWith("\n\n"))
            {
                // Ignore
            }
            else if (last2.EndsWith("\n"))
            {
                Out.Append("\n");
            }
            else
            {
                Out.Append("\n\n");
            }
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
                if (c == '*' || c == '_' || c == '#' || c == '`' || c == '[' || c == ']')
                {
                    sb.Append('\\');
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        #endregion
    }
}
