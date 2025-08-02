using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace m.format.conv
{
    /// <summary>
    /// Converts AmigaGuide to HTML.
    /// </summary>
    /// <version>2.1.0</version>
    /// <date>2025-08-02</date>
    /// <author>Miloš Perunović</author>
    public class ConvGuideHtml
    {
        #region Main methods for converting Markdown to HTML

        /// <summary>
        /// Converts AmigaGuide document to HTML.
        /// </summary>
        /// <param name="guide">The AmigaGuide document as a string.</param>
        /// <param name="lang">Language code (e.g. "en", "cnr")</param>
        /// <param name="head">Additional head elements (e.g. CSS links)</param>
        /// <returns>HTML representation of the AmigaGuide document</returns>
        public static string Convert(string guide, string lang = "en", string head = null)
        {
            Stopwatch sw = Stopwatch.StartNew();

            // Convert AmigaGuide to HTML body
            // This method will also extract metadata from the document, such as title.
            string body = new ConvGuideHtml().ToHtmlBody(guide, out Dictionary<string, string> metadata);

            // Generate html meta tags from metadata
            StringBuilder meta = new StringBuilder();
            if (!metadata.ContainsKey("title")) { metadata["title"] = "Untitled Document"; }
            foreach (KeyValuePair<string, string> pair in metadata)
            {
                if (pair.Key == "title")
                {
                    meta.Append($"  <title>{EscapeHtml(pair.Value)}</title>\n");
                }
                else
                {
                    meta.Append($"  <meta name=\"{EscapeHtml(pair.Key)}\" content=\"{EscapeHtml(pair.Value)}\">\n");
                }
            }

            sw.Stop();
            double seconds = (double)sw.ElapsedTicks / Stopwatch.Frequency;
            Console.WriteLine($"AmigaGuide -> HTML conv.: {seconds} sec.");

            return
                "<!DOCTYPE html>\n" +
                $"<html lang=\"{lang}\">\n" +
                "<head>\n" +
                "  <meta charset=\"utf-8\">\n" +
                (meta.Length > 0 ? meta.ToString() : "") +
                (head ??
                "  <style>\n" +
                "    .btn {\n" +
                "    display: inline-block;\n" +
                "    padding: 3px 7px;\n" +
                "    background: #eee;\n" +
                "    border: 1px solid #ccc;\n" +
                "    text-decoration: none;\n" +
                "    color: #333;\n" +
                "    }\n" +
                "    .btn:hover {\n" +
                "      background: #2f8bc1;\n" +
                "    }\n" +
                "  </style>\n"
                ) +
                "</head>\n" +
                "<body>\n" +
                $"{body}" +
                "</body>\n" +
                "</html>\n";
        }

        /// <summary>
        /// StringBuilder for accumulating the HTML output.
        /// This is used to build the final HTML string efficiently.
        /// </summary>
        private StringBuilder Out;

        private StringBuilder Buffer;

        /// <summary>
        /// Dictionary to hold metadata extracted from the AmigaGuide document.
        /// </summary>
        private readonly Dictionary<string, string> Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Converts an AmigaGuide document to HTML.
        /// </summary>
        /// <param name="doc">AmigaGuide document</param>
        /// <param name="metadata">Document title.</param>
        /// <returns>HTML representation of the AmigaGuide document</returns>
        private string ToHtmlBody(string doc, out Dictionary<string, string> metadata)
        {
            int len = doc.Length;
            Out = new StringBuilder(len * 2); // Pre-allocate space for the HTML output
            Buffer = new StringBuilder();
            Out.Append("<pre>\n");

            for (int pos = 0; pos < len; pos++)
            {
                char c = doc[pos];

                switch (c)
                {
                    case '\r':
                        break;

                    case '<':
                        Out.Append("&lt;");
                        break;

                    case '>':
                        Out.Append("&gt;");
                        break;

                    case '&':
                        Out.Append("&amp;");
                        break;

                    case '\\':
                        if (pos < len - 1)
                        {
                            if (doc[pos + 1] == '\\')
                            {
                                Out.Append("\\");
                                pos++;
                            }
                            else if (doc[pos + 1] == '@')
                            {
                                Out.Append("@");
                                pos++;
                            }
                        }
                        break;

                    case '@':
                        ProcessCommand(doc, len, ref pos);
                        break;

                    default:
                        if (inNode)
                        {
                            Out.Append(c);
                        }
                        else
                        {
                            Buffer.Append(c);
                        }
                        break;
                }
            }

            Out.Append("</pre>\n");

            metadata = Metadata;
            return Out.ToString();
        }

        /// <summary>
        /// Creates an HTML link with a button style.
        /// The link will point to an anchor with the specified href.
        /// </summary>
        private static string CreateLink(string href, string txt)
        {
            return $"<a href=\"#{href.ToLower().Replace(' ', '_')}\" class=\"btn\">{EscapeHtml(txt)}</a>";
        }

        #endregion

        #region Command processing methods

        /// <summary>
        /// Flag to indicate if we are currently inside a node.
        /// </summary>
        private bool inNode = false;

        /// <summary>
        /// Processes a command in the AmigaGuide document.
        /// This method handles commands like `@node`, `@toc`, `@title`, etc
        /// </summary>
        /// <param name="doc">The AmigaGuide document</param>
        /// <param name="len">The length of the document</param>
        /// <param name="pos">The current position in the document</param>
        /// <returns>The processed command as a string</returns>
        private void ProcessCommand(string doc, int len, ref int pos)
        {
            string res = "";

            if (++pos < len && doc[pos] == '{')
            {
                // Attributes command.
                while (++pos < len && doc[pos] != '}')
                {
                    res += doc[pos];
                }

                if (res == "i" || res == "b" || res == "u")
                {
                    res = $"<{res}>";
                }
                else if (res == "ui" || res == "ub" || res == "uu")
                {
                    res = $"</{res.Substring(1, 1)}>";
                }
                else if (res.Length > 0 && res[0] == '"')
                {
                    string[] args = ParseArguments(res);
                    if (args.Length > 2)
                    {
                        res = CreateLink(args[2], args[0]);
                    }
                }
                else
                {
                    res = "⚠️ Unknown attribute: " + res;
                }
            }
            else
            {
                // Global commands.
                pos--;
                string cmdA = "";
                while (++pos < len && doc[pos] != '\n' && doc[pos] != '\r')
                {
                    cmdA += doc[pos];
                }

                string argLine = "";
                int j = cmdA.IndexOf(' ');
                if (j != -1)
                {
                    argLine = cmdA.Substring(j + 1);
                    cmdA = cmdA.Substring(0, j);
                }

                string[] args = ParseArguments(argLine);

                string cmd = cmdA.ToLower();

                switch (cmd)
                {
                    case "node":
                        if (Buffer.Length > 1)
                        {
                            string buff = Buffer.ToString().Trim(' ', '\n', '\r');
                            if (buff.Length > 0)
                            {
                                Out.Append($"<!-- guide-preamble: {buff} -->");
                            }
                            Buffer.Clear();
                        }
                        if (args.Length > 0)
                        {
                            inNode = true;
                            res = "<a id=\"" + args[0].ToLower().Replace(' ', '_') + "\"></a>";
                            if (args.Length > 1)
                            {
                                string s = args[1] ?? args[0];
                                res += $"</pre>\n<h2>{EscapeHtml(s)}</h2>\n<pre>";
                            }
                        }

                        break;

                    case "endnode":
                        inNode = false;
                        res = "</pre>\n<hr>\n<pre>\n";
                        break;

                    default:
                        if (args.Length > 0)
                        {
                            switch (cmd)
                            {
                                case "toc":
                                case "prev":
                                case "next":
                                    string txt = cmd.Replace("toc", "Contents").Replace("prev", "Browse <").Replace("next", "Browse >");
                                    res = CreateLink(args[0], txt);
                                    break;
                                case "database":
                                    Metadata["title"] = args[0];
                                    break;
                                case "master":
                                case "width":
                                case "wordwrap":
                                case "smartwrap":
                                    res = $"<!-- ignored-command: @{cmdA} {EscapeHtml(argLine)} -->";
                                    break;
                                case "title":
                                    Metadata["title"] = args[0];
                                    res = $"</pre>\n<h1>{EscapeHtml(args[0])}</h1>\n<pre>\n";
                                    break;
                                case "author":
                                    Metadata["author"] = args[0];
                                    break;
                                case "rem":
                                case "remark":
                                    res = $"<!--{EscapeHtml(argLine)}-->\n";
                                    break;
                                default:
                                    res = $"@{$"{cmdA} {EscapeHtml(argLine)}".Trim()}\n";
                                    break;
                            }
                        }
                        else
                        {
                            res = $"@{$"{cmdA} {EscapeHtml(argLine)}".Trim()}\n";
                        }

                        break;
                }
            }
            Out.Append(res);
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
        /// Parses the arguments from a string input.
        /// Arguments are separated by spaces, and can be enclosed in quotes.
        /// </summary>
        /// <param name="input">The input string containing arguments.</param>
        /// <returns>An array of parsed arguments.</returns>
        private static string[] ParseArguments(string input)
        {
            List<string> result = new List<string>();

            int i = 0;
            while (i < input.Length)
            {
                // Skip all whitespace characters
                while (i < input.Length && Char.IsWhiteSpace(input[i])) { i++; }

                if (i >= input.Length) { break; }

                string arg;

                if (input[i] == '\"')
                {
                    // Starts quoted argument
                    i++; // Skip first quote
                    int start = i;

                    while (i < input.Length && input[i] != '\"') { i++; }

                    arg = input.Substring(start, i - start);

                    if (i < input.Length && input[i] == '\"')
                    {
                        i++; // Skip closing quote
                    }
                }
                else
                {
                    int start = i;

                    while (i < input.Length && !char.IsWhiteSpace(input[i])) { i++; }

                    arg = input.Substring(start, i - start);
                }

                result.Add(arg);
            }

            return result.ToArray();
        }

        #endregion
    }
}
