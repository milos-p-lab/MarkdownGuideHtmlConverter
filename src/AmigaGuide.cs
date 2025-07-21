using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace m.format.conv
{
    /// <summary>
    /// Converts AmigaGuide documents.
    /// </summary>
    /// <version>1.4.0</version>
    /// <date>2025-07-21</date>
    /// <author>Miloš Perunović</author>
    public static class AmigaGuide
    {
        /// <summary>
        /// Converts AmigaGuide document to HTML.
        /// </summary>
        /// <param name="doc">The AmigaGuide document as a string.</param>
        /// <param name="lang">Language code (e.g. "en", "cnr")</param>
        /// <param name="head">Additional head elements (e.g. CSS links)</param>
        /// <returns>HTML representation of the markdown</returns>
        public static string ToHtml(string doc, string lang = "en", string head = null)
        {
            Stopwatch sw = Stopwatch.StartNew();

            // Convert AmigaGuide to HTML body
            // This method will also extract metadata from the document, such as title.
            string body = ToHtmlBody(doc, out Dictionary<string, string> metadata);

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

            sw.Stop();
            double seconds = (double)sw.ElapsedTicks / Stopwatch.Frequency;
            Console.WriteLine($"AmigaGuide -> HTML conv.: {seconds} sec.");

            return
                "<!DOCTYPE html>\n" +
                $"<html lang=\"{lang}\">\n" +
                "<head>\n" +
                "<meta charset=\"utf-8\">" +
                (meta.Length > 0 ? meta.ToString() : "") +
                (head ?? "") +
                "</head>\n" +
                "<body>\n" +
                $"{body}" +
                "</body>\n" +
                "</html>\n";
        }

        /// <summary>
        /// Converts an AmigaGuide document to HTML.
        /// </summary>
        /// <param name="doc">AmigaGuide document</param>
        /// <param name="metadata">Document title.</param>
        public static string ToHtmlBody(string doc, out Dictionary<string, string> metadata)
        {
            int len = doc.Length;
            StringBuilder body = new StringBuilder(len * 2); // Pre-allocate space for the HTML output
            metadata = new Dictionary<string, string>();

            bool skip = true;
            bool btnContents = true;
            bool nodeMain = true;

            body.Append("<div style=\"white-space: pre; font-family: monospace;\">\n");

            for (int i = 0; i < len; i++)
            {
                char c = doc[i];
                string tag = "";
                string cmd = "";

                switch (c)
                {
                    case '\r':
                        break;

                    case '<':
                        body.Append("&lt;");
                        break;

                    case '>':
                        body.Append("&gt;");
                        break;

                    case '&':
                        body.Append("&amp;");
                        break;

                    case '\\':
                        if (i < len - 1)
                        {
                            if (doc[i + 1] == '\\')
                            {
                                body.Append("\\");
                                i++;
                            }
                            else if (doc[i + 1] == '@')
                            {
                                body.Append("@");
                                i++;
                            }
                        }
                        break;

                    case '@':
                        {
                            if (++i < len && doc[i] == '{')
                            {
                                // Extracting the tag.
                                while (++i < len && doc[i] != '}')
                                {
                                    tag += doc[i];
                                }

                                if (tag == "i" || tag == "b" || tag == "u")
                                {
                                    tag = $"<{tag}>";
                                }
                                else if (tag == "ui" || tag == "ub" || tag == "uu")
                                {
                                    tag = $"</{tag.Substring(1, 1)}>";
                                }
                                else if (tag[0] == '"')
                                {
                                    string[] args = ParseArguments(tag);
                                    if (args.Length > 2)
                                    {
                                        tag = CreateButton(args[2], args[0]);
                                    }
                                }
                            }
                            else
                            {
                                // Extracting the command.

                                i--;
                                int ti = i;
                                while (++i < len && doc[i] != '\n' && doc[i] != '\r')
                                {
                                    cmd += doc[i];
                                }

                                string argLine = "";
                                int j = cmd.IndexOf(' ');
                                if (j == -1)
                                {
                                    cmd = cmd.ToLower();
                                }
                                else
                                {
                                    argLine = cmd.Substring(j + 1);
                                    cmd = cmd.Substring(0, j).ToLower();
                                }

                                string[] args = ParseArguments(argLine);

                                if (cmd == "node")
                                {
                                    btnContents = true;
                                    skip = false;
                                    if (args.Length > 0)
                                    {
                                        body.Append("<a id=\"" + args[0].ToLower().Replace(' ', '_') + "\"></a>");
                                        if (args.Length > 1 && !nodeMain)
                                        {
                                            string s = args[1] ?? args[0];
                                            body.Append($"<h2>{s}</h2>");
                                        }
                                    }
                                }
                                else if (cmd == "endnode")
                                {
                                    skip = true;
                                    if (btnContents && !nodeMain)
                                    {
                                        tag = CreateButton("main", "Contents");
                                    }
                                    tag += "<hr>\n";
                                    nodeMain = false;
                                }
                                else if ((cmd == "toc" || cmd == "prev" || cmd == "next") && args.Length > 0)
                                {
                                    btnContents = false;
                                    string txt = cmd.Replace("toc", "Contents").Replace("prev", "Browse <").Replace("next", "Browse >");
                                    tag = CreateButton(args[0], txt);
                                }
                                else if (cmd == "database" && args.Length > 0)
                                {
                                    metadata["title"] = WebUtility.HtmlEncode(args[0]);
                                }
                                else if (cmd == "title" && args.Length > 0)
                                {
                                    if (nodeMain)
                                    {
                                        metadata["title"] = WebUtility.HtmlEncode(args[0]);
                                    }
                                    body.Append($"<h2>{args[0]}</h2>");
                                }
                                else
                                {
                                    // Unknown command or encoding error.
                                    tag = "";
                                    i = ti;
                                    if (!skip)
                                    {
                                        // Encoding error.
                                        body.Append("@");
                                    }
                                    else
                                    {
                                        // Unknown command is passed as a comment.
                                        body.Append($"<!--@{cmd}{(argLine.Length > 0 ? " " + WebUtility.HtmlEncode(argLine) : "")}-->\n");
                                    }
                                }
                            }

                            body.Append(tag);
                            break;
                        }

                    default:
                        if (!skip)
                        {
                            body.Append(c);
                        }
                        break;
                }
            }

            body.Append("</div>\n");

            return body.ToString();
        }

        /// <summary>
        /// Creates an HTML button element.
        /// </summary>
        private static string CreateButton(string href, string txt)
        {
            return $"<input type=\"button\" onclick=\"location.href='#{href.ToLower().Replace(' ', '_')}';\" value=\"{WebUtility.HtmlEncode(txt)}\">";
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
                // skip all whitespace characters
                while (i < input.Length && Char.IsWhiteSpace(input[i])) { i++; }

                if (i >= input.Length) { break; }

                string arg;

                if (input[i] == '\"')
                {
                    // starts quoted argument
                    i++; // skip first quote
                    int start = i;

                    while (i < input.Length && input[i] != '\"') { i++; }

                    arg = input.Substring(start, i - start);

                    if (i < input.Length && input[i] == '\"')
                    {
                        i++; // skip closing quote
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
    }
}
