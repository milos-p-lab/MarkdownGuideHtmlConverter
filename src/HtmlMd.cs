using System;
using System.Diagnostics;
using System.Text;

namespace m.format.conv
{
    /// <summary>
    /// Converts HTML to Markdown.
    /// </summary>
    /// <version>1.4.0</version>
    /// <date>2025-07-21</date>
    /// <author>Miloš Perunović</author>
    public static class HtmlMd
    {
        #region Main methods for converting HTML to Markdown

        /// <summary>
        /// Converts HTML document to Markdown.
        /// </summary>
        /// <param name="html">The HTML document as a string.</param>
        /// <returns>Markdown representation of the HTML</returns>
        public static string ToMarkdown(string html)
        {
            Stopwatch sw = Stopwatch.StartNew();

            int len = html.Length;
            StringBuilder sb = new StringBuilder(len); // Pre-allocate space for the Markdown output
            StringBuilder textBuffer = new StringBuilder();

            bool spc = false;

            bool inPar = false, inPre = false, inList = false,
                 inBlockquote = false, contBlockquote = true, firstLineBlockquote = true;

            int listLevel = 0;

            for (int p = 0; p < len; p++)
            {
                char c = html[p];

                // Check for opening tag
                if (c == '\n' || c == '\r')
                {
                    // Ignores newlines in the HTML
                }
                else if (c == '<')
                {
                    // Parses an HTML tag from the given position in the HTML string.
                    int start = p;
                    while (p < html.Length && html[p] != '>')
                    {
                        p++;
                    }
                    string tag = html.Substring(start, p - start + 1);
                    string tagLow = tag.ToLower();

                    // Write the accumulated text to the Markdown output
                    string content = textBuffer.ToString().Trim();
                    if (spc)
                    {
                        sb.Append(' ');
                    }
                    spc = false;
                    if (content.Length > 0)
                    {
                        if (inBlockquote && !contBlockquote)
                        {
                            sb.Append("> ");
                            contBlockquote = true;
                            firstLineBlockquote = false;
                        }
                        sb.Append(EscapeMarkdownChars(content));
                    }
                    textBuffer.Clear();

                    // Paragraph handling
                    if (tag.Length >= 3 && tagLow[1] == 'p' && (tag[2] == '>' || tag[2] == ' '))
                    {
                        inPar = true;
                        if (inBlockquote)
                        {
                            if (!firstLineBlockquote)
                            {
                                sb.Append("\n>\n");
                            }
                        }
                        else
                        {
                            sb.Append("\n");
                        }
                    }

                    // Heading handling
                    else if (tag.Length >= 3 && tagLow[1] == 'h' && char.IsDigit(tag[2]))
                    {
                        inPar = false;

                        int level = tag[2] - '0';
                        if (sb.Length > 1 && (sb[sb.Length - 1] == '\n' || sb[sb.Length - 1] == ' ') && sb[sb.Length - 2] != '\n')
                        {
                            sb.Append('\n');
                        }
                        sb.Append($"{new string('#', level)} ");

                        //string id = GetAttribute(tag, "id");
                        //if (!string.IsNullOrEmpty(id))
                        //{
                        //    sb.Append($"<a id=\"{id}\"></a> ");
                        //}
                    }
                    else
                    {
                        char lastChr = sb.Length > 0 ? sb[sb.Length - 1] : '\0';
                        char nextChr = p < len ? html[p + 1] : '\0';

                        // Handle different HTML tags
                        switch (tagLow)
                        {
                            // Paragraphs
                            case "</p>":
                                inPar = false;
                                if (inBlockquote)
                                {
                                    contBlockquote = false;
                                }
                                else
                                {
                                    sb.Append("\n");
                                }
                                break;

                            // Basic text formatting
                            case "<strong>":
                            case "<b>":
                                if (p + 1 < len && lastChr != ' ')
                                {
                                    sb.Append(' ');
                                }
                                sb.Append("**");
                                break;
                            case "</strong>":
                            case "</b>":
                                sb.Append("**");
                                if (lastChr != ' ' && nextChr != '<')
                                {
                                    spc = true;
                                }
                                break;
                            case "<em>":
                            case "<i>":
                                if (p + 1 < len && lastChr != ' ' && lastChr != '*')
                                {
                                    sb.Append(' ');
                                }
                                sb.Append("*");
                                break;
                            case "</em>":
                            case "</i>":
                                sb.Append("*");
                                if (lastChr != ' ' && nextChr != '<')
                                {
                                    spc = true;
                                }
                                break;

                            // Lists
                            case "<ul>":
                            case "<ol>":
                                inList = true;
                                listLevel++;
                                break;
                            case "</ul>":
                            case "</ol>":
                                listLevel--;
                                if (listLevel == 0) { inList = false; }
                                sb.Append('\n');
                                break;
                            case "<li>":
                                sb.Append(inList ? $"\n{new string(' ', (listLevel - 1) * 2)}- " : "\n- ");
                                break;
                            case "</li>":
                                break;

                            // Headings
                            case "</h1>":
                            case "</h2>":
                            case "</h3>":
                                sb.Append("\n"); break;

                            // Code/preformatted
                            case "<pre>":
                                inPre = true;
                                sb.Append("\n```\n");
                                break;
                            case "</pre>":
                                inPre = false;
                                sb.Append("\n```\n");
                                break;
                            case "<code>":
                            case "</code>":
                                sb.Append(inPre ? "" : "`");
                                break;

                            // Blockquotes
                            case "<blockquote>":
                                inBlockquote = true;
                                firstLineBlockquote = true;
                                contBlockquote = false;
                                sb.Append('\n');
                                break;
                            case "</blockquote>":
                                inBlockquote = false;
                                contBlockquote = true;
                                sb.Append('\n');
                                break;

                            // Links
                            case "<a ":
                                string href = GetAttribute(tag, "href");
                                sb.Append($"[{content}]({href})");
                                break;

                            // Images
                            case "<img ":
                                string src = GetAttribute(tag, "src");
                                string alt = GetAttribute(tag, "alt");
                                sb.Append($"![{alt}]({src})");
                                break;

                            // Line breaks
                            case "<br>":
                            case "<br/>":
                            case "<br />":
                                if (inPar) { sb.Append("  "); }
                                sb.Append("\n");
                                break;

                            // Horizontal rule
                            case "<hr>":
                            case "<hr/>":
                            case "<hr />":
                                sb.Append("\n---\n");
                                break;

                            default:
                                if (tag.StartsWith("</") || (lastChr != ' ' && lastChr != '\n'))
                                {
                                    spc = true;
                                }
                                break;
                        }
                    }
                }
                else if (!inPre && c == ' ')
                {
                    // Whitespace normalization
                    if (textBuffer.Length > 0 && !char.IsWhiteSpace(textBuffer[textBuffer.Length - 1]))
                    {
                        textBuffer.Append(' ');
                    }
                }
                else
                {
                    textBuffer.Append(c);
                }
            }

            if (textBuffer.Length > 0)
            {
                sb.Append(textBuffer.ToString().Trim());
            }

            sw.Stop();
            double seconds = (double)sw.ElapsedTicks / Stopwatch.Frequency;
            Console.WriteLine($"HTML -> Markdown conv.: {seconds} sec.");

            return sb.ToString();
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
