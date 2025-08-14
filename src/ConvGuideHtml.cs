using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace m.format.conv
{
    /// <summary>
    /// Converts AmigaGuide to HTML.
    /// </summary>
    /// <version>2.3.0</version>
    /// <date>2025-08-07</date>
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
        /// <param name="ignoreWarnings">Whether to ignore warnings during conversion</param>
        /// <returns>HTML representation of the AmigaGuide document</returns>
        public static string Convert(string guide, string lang = "en", string head = null, bool ignoreWarnings = false)
        {
            Stopwatch sw = Stopwatch.StartNew();

            // Convert AmigaGuide to HTML body
            // This method will also extract metadata from the document, such as title.
            string body = new ConvGuideHtml().ToHtmlBody(guide, out Dictionary<string, string> metadata, ignoreWarnings);

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

        private StringBuilder TextBuffer;

        /// <summary>
        /// Current line number.
        /// </summary>
        private int LineNum = 1;

        /// <summary>
        /// Dictionary to hold metadata extracted from the AmigaGuide document.
        /// </summary>
        private readonly Dictionary<string, string> Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Converts an AmigaGuide document to HTML.
        /// </summary>
        /// <param name="doc">AmigaGuide document</param>
        /// <param name="metadata">Metadata extracted from the document</param>
        /// <param name="ignoreWarnings">Whether to ignore warnings during conversion</param>
        /// <returns>HTML representation of the AmigaGuide document</returns>
        private string ToHtmlBody(string doc, out Dictionary<string, string> metadata, bool ignoreWarnings)
        {
            int len = doc.Length;
            Out = new StringBuilder(len * 2); // Pre-allocate space for the HTML output
            TextBuffer = new StringBuilder();

            Out.Append("<pre>\n");

            for (int pos = 0; pos < len; pos++)
            {
                char c = doc[pos];

                // Count new lines
                if (c == '\n') { LineNum++; }

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
                            TextBuffer.Append(c);
                        }
                        break;
                }
            }

            Out.Append("</pre>\n");

            CloseUnclosedTags(Out);

            // Write any buffered text to the output.
            // This is necessary to ensure that any text accumulated in the TextBuffer is written to the output.
            WriteBufferedText();

            if (!ignoreWarnings)
            {
                GenerateWarningsReport();
            }

            metadata = Metadata;
            return Out.ToString();
        }

        /// <summary>
        /// Creates an HTML link with a button style.
        /// The link will point to an anchor with the specified href.
        /// </summary>
        /// <param name="linkType">Type of the link (e.g. "link", "alink", "system")</param>
        /// <param name="href">The href attribute for the link</param>
        /// <param name="text">The text to display for the link</param>
        /// <returns>An HTML anchor element with the specified href and text</returns>
        private string CreateLink(string linkType, string href, string text)
        {
            string link = linkType.Trim().ToLower();
            switch (link)
            {
                // Link to another node in the same document or another document
                case "link":
                case "alink":
                    {
                        try
                        {
                            string path = href;
                            string node = "";
                            string ext = Path.GetExtension(path).ToLower();
                            int i = path.LastIndexOf('/');
                            if (i > -1)
                            {
                                // Link to a node in another document
                                node = path.Substring(i + 1).ToLower().Replace(' ', '_');
                                path = path.Substring(0, i);
                                path = Path.ChangeExtension(path, "html");
                                node = "#" + href.Substring(i + 1).ToLower().Replace(' ', '_');
                            }
                            else if (ext == ".guide")
                            {
                                // If the path is a .guide file, change it to .html
                                path = Path.ChangeExtension(path, "html");
                            }
                            else
                            {
                                // If the path does not contain a '/', and is not a .guide file,
                                // treat it as a node in the current document
                                node = "#" + path.ToLower().Replace(' ', '_');
                                path = "";
                            }
                            return $"<a href=\"{path}{EscapeHtml(node)}\" class=\"btn\">{EscapeHtml(text)}</a>";
                        }
                        catch (Exception ex)
                        {
                            ReportWarning(ex.Message);
                            return $"<a href=\"#{href.ToLower().Replace(' ', '_')}\" class=\"btn\">{EscapeHtml(text)}</a>";
                        }
                    }

                // Execute system command
                case "system":
                    {
                        string path = href;
                        string[] a = path.Split(' ');

                        if (a.Length > 1)
                        {
                            path = path.Substring(a[0].Length).Trim();
                        }
                        return $"<a href=\"{path}\" class=\"btn\">{EscapeHtml(text)}</a>";
                    }

                // Unknown link type
                default:
                    ReportWarning("Unknown link type: " + link);
                    return $"<a href=\"#{href.ToLower().Replace(' ', '_')}\" class=\"btn\">{EscapeHtml(text)}</a>";
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

        /// <summary>
        /// Writes the buffered text to the output.
        /// This method is called to flush the accumulated text buffer into the HTML output.
        /// </summary>
        private void WriteBufferedText()
        {
            if (TextBuffer.Length > 1)
            {
                string buff = TextBuffer.ToString().Trim(' ', '\n', '\r');
                if (buff.Length > 0)
                {
                    Out.Append($"<!-- pre/post-node text (guide preamble): {buff} -->");
                }
                TextBuffer.Clear();
            }
        }

        #endregion

        #region Command processing methods

        /// <summary>
        /// Flag to indicate if we are currently inside a node.
        /// </summary>
        private bool inNode;

        /// <summary>
        /// Counters for the number of open tags.
        /// </summary>
        private int CntBld, CntItl, CntUnd;

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
            int start = pos;

            if (++pos < len && doc[pos] == '{')
            {
                // ======== Attributes command ========
                string atr = "";
                while (++pos < len && doc[pos] != '}')
                {
                    atr += doc[pos];
                }

                switch (atr.ToLower())
                {
                    case "b":
                        if (CntBld <= 0) { CntBld++; res = "<strong>"; }
                        else { ReportWarning("Repeated bold tag"); }
                        break;
                    case "ub":
                        if (CntBld > 0) { CntBld--; res = "</strong>"; }
                        else { ReportWarning("Closing bold tag without opening"); }
                        break;

                    case "i":
                        if (CntItl <= 0) { CntItl++; res = "<em>"; }
                        else { ReportWarning("Repeated italic tag"); }
                        break;
                    case "ui":
                        if (CntItl > 0) { CntItl--; res = "</em>"; }
                        else { ReportWarning("Closing italic tag without opening"); }
                        break;

                    case "u":
                        if (CntUnd <= 0) { CntUnd++; res = "<u>"; }
                        else { ReportWarning("Repeated underline tag"); }
                        break;
                    case "uu":
                        if (CntUnd > 0) { CntUnd--; res = "</u>"; }
                        else { ReportWarning("Closing underline tag without opening"); }
                        break;

                    case "plain":
                        CloseUnclosedTags(Out, false);
                        break;

                    default:
                        if (atr.Length > 0 && atr[0] == '"')
                        {
                            string[] args = ParseArguments(atr);
                            if (args.Length > 2)
                            {
                                res = CreateLink(linkType: args[1], href: args[2], text: args[0]);
                            }
                            else
                            {
                                ReportWarning($"Unknown Attribute command: @{{{atr}}}");
                                if (pos == len) { pos = start; return; }
                            }
                        }
                        else
                        {
                            ReportWarning($"Unknown Attribute command: @{{{atr}}}");
                            if (pos == len) { pos = start; return; }
                        }
                        break;
                }
            }
            else
            {
                // ===== Global and node commands =====
                pos--;
                string cmdA = "";
                while (++pos < len && doc[pos] != '\n' && doc[pos] != '\r')
                {
                    cmdA += doc[pos];
                }

                // Count new lines
                if (pos < len && doc[pos] == '\n') { LineNum++; }

                string argLine = "";
                int i = cmdA.IndexOf(' ');
                if (i != -1)
                {
                    argLine = cmdA.Substring(i + 1);
                    cmdA = cmdA.Substring(0, i);
                }
                string cmd = cmdA.ToLower();

                string[] args = ParseArguments(argLine);
                string arg = args.Length > 0 ? args[0] : "";

                bool closeUnclosedTags = true;

                switch (cmd)
                {
                    // ======== Node commands ========
                    case "node":
                        WriteBufferedText();
                        if (args.Length > 0)
                        {
                            inNode = true;
                            res = "<a id=\"" + arg.ToLower().Replace(' ', '_') + "\"></a>";
                            if (args.Length > 1)
                            {
                                string s = args[1] ?? arg;
                                res += $"</pre>\n<h2>{EscapeHtml(s)}</h2>\n<pre>";
                            }
                        }
                        break;
                    case "endnode":
                        inNode = false;
                        res = "</pre>\n<hr>\n<pre>\n";
                        break;

                    case "toc":
                    case "prev":
                    case "next":
                        string text = cmd.Replace("toc", "Contents").Replace("prev", "Browse <").Replace("next", "Browse >");
                        res = CreateLink(linkType: "Link", href: arg, text: text);
                        break;

                    // ======== Global commands ========
                    case "database":
                        Metadata["title"] = arg;
                        break;
                    case "master":
                    case "help":
                    case "index":
                    case "width":
                    case "wordwrap":
                    case "smartwrap":
                    case "font":
                        res = $"<!-- ignored-command: @{cmdA} {EscapeHtml(argLine)} -->";
                        break;

                    case "title":
                        Metadata["title"] = arg.Trim('"', ' ');
                        res = $"</pre>\n<h1>{EscapeHtml(arg)}</h1>\n<pre>\n";
                        break;
                    case "$ver:":
                        Metadata["version"] = arg.Trim('"', ' ');
                        break;
                    case "author":
                        Metadata["author"] = arg.Trim('"', ' ');
                        break;
                    case "(c)":
                        Metadata["copyright"] = arg.Trim('"', ' ');
                        break;

                    case "rem":
                    case "remark":
                        res = $"<!--{EscapeHtml(argLine)}-->\n";
                        break;

                    default:
                        res = $"@{$"{cmdA} {EscapeHtml(argLine)}".Trim()}\n";
                        closeUnclosedTags = false;
                        break;
                }
                if (closeUnclosedTags)
                {
                    CloseUnclosedTags(Out);
                }
            }
            Out.Append(res);
        }

        /// <summary>
        /// Closes any unclosed tags in the HTML output, and reports warnings for each unclosed tag.
        /// This method ensures that all opened tags are properly closed before the end of the document.
        /// </summary>
        private void CloseUnclosedTags(StringBuilder sb, bool report = true)
        {
            while (CntUnd > 0)
            {
                sb.Append("</u>");
                if (report) { ReportWarning("Unclosed underline tag", before: true); }
                CntUnd--;
            }
            while (CntItl > 0)
            {
                sb.Append("</em>");
                if (report) { ReportWarning("Unclosed italic tag", before: true); }
                CntItl--;
            }
            while (CntBld > 0)
            {
                sb.Append("</strong>");
                if (report)
                { ReportWarning("Unclosed bold tag", before: true); }
                CntBld--;
            }
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
        private void ReportWarning(string desc, bool before = false)
        {
            Warnings.Add((before ? "Line &lt;= " : "Line ") + LineNum + ": " + EscapeHtml(desc));
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
