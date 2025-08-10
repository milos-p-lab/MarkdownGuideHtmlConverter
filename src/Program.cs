using m.format.conv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace mdoc
{
    internal static class Program
    {
        /// <summary>
        /// This program converts files between different formats:
        /// - Markdown (.md) to HTML (.html)
        /// - HTML (.html) to Markdown (.md)
        /// - AmigaGuide (.guide) to HTML (.html)
        /// - AmigaGuide (.guide) to Markdown (.md)
        /// - Text (.txt) to HTML (.html)
        /// </summary>
        /// <param name="args">Command line arguments: input file path and optional output file path.</param>
        /// <remarks>
        /// If no output file path is provided, the output file will have the same name as the input file,
        /// but with the appropriate extension based on the conversion direction.
        /// </remarks>
        /// <version>2.3.1</version>
        /// <date>2025-08-10</date>
        /// <author>Miloš Perunović</author>
        private static void Main(string[] args)
        {
#if DEBUG
            args = new string[2];
            args[0] = @"R:\Temp\test.txt";
            args[1] = @"R:\Temp\test.md";
            //args[1] = "--encoding=windows-1250";
#endif
            // Parse switches
            bool ignoreWarnings = false;
            Encoding oldEncoding = Encoding.GetEncoding("windows-1252");

            List<string> remainingArgs = new List<string>();
            foreach (string arg in args)
            {
                if (arg == "--ignore-warnings")
                {
                    ignoreWarnings = true;
                }
                else if (arg.StartsWith("--encoding="))
                {
                    try
                    {
                        oldEncoding = Encoding.GetEncoding(arg.Substring(11));
                    }
                    catch
                    {
                        Console.WriteLine(
                            $"Error: Unsupported encoding specified: {arg.Substring(11)}\n" +
                            "Supported encodings include: windows-1252, utf-8, ascii, etc.");
                        return;
                    }
                }
                else if (arg.StartsWith("--"))
                {
                    Console.WriteLine($"Error: Unsupported switch: {arg}");
                    return;
                }
                else
                {
                    remainingArgs.Add(arg);
                }
            }

            args = remainingArgs.ToArray();

            if (args.Length < 1)
            {
                Console.WriteLine(
                    "Usage: mdoc <input_file> [output_file]\n" +
                    "Supported conversions:\n" +
                    "  .md    -> .html\n" +
                    "  .html  -> .md\n" +
                    "  .guide -> .html\n" +
                    "  .guide -> .md\n" +
                    "  .txt   -> .html (smart)\n" +
                    "  .txt   -> .md (smart)"
                    );
                return;
            }

            string inPath = args[0];
            string outPath;

            if (!File.Exists(inPath))
            {
                Console.WriteLine($"Error: Input file not found: {inPath}");
                return;
            }

            string inExt = Path.GetExtension(inPath).ToLowerInvariant();
            string outContent;
            string outExt;

            try
            {
                // Determine output file path
                if (args.Length > 1)
                {
                    outPath = args[1];
                    outExt = Path.GetExtension(outPath).ToLowerInvariant();
                    outPath = Path.ChangeExtension(outPath, outExt);
                }
                else
                {
                    // No output file specified, determine based on input file extension
                    switch (inExt)
                    {
                        case ".md":
                            outExt = ".html";
                            break;
                        case ".html":
                            outExt = ".md";
                            break;
                        case ".guide":
                        case ".txt":
                            outExt = ".html";
                            break;
                        default:
                            outExt = "?";
                            break;
                    }
                    outPath = Path.ChangeExtension(inPath, outExt);
                }

                Encoding enc = Encoding.UTF8;
                switch (inExt)
                {
                    case ".guide":
                        enc = oldEncoding;
                        break;
                    case ".txt":
                        enc = DetectEncoding(inPath, oldEncoding);
                        Console.WriteLine($"Detected encoding for {inPath}: {enc.WebName}");
                        break;
                }

                // Read the input file content
                string inContent = File.ReadAllText(inPath, enc);

                // Perform conversion based on input and output extensions
                string convDirection = $"{inExt} -> {outExt}";
                switch (convDirection)
                {
                    case ".md -> .html":
                        outContent = ConvMarkdownHtml.Convert(inContent, ignoreWarnings: ignoreWarnings);
                        break;
                    case ".html -> .md":
                        outContent = ConvHtmlMarkdown.Convert(inContent, ignoreWarnings: ignoreWarnings);
                        break;
                    case ".guide -> .html":
                        outContent = ConvGuideHtml.Convert(inContent, ignoreWarnings: ignoreWarnings);
                        break;
                    case ".guide -> .md":
                        outContent = ConvHtmlMarkdown.Convert(ConvGuideHtml.Convert(inContent), ignoreWarnings: true, markCodeBlock: false);
                        break;
                    case ".txt -> .html":
                        outContent = ConvMarkdownHtml.SmartTxtConvert(inContent);
                        break;
                    case ".txt -> .md":
                        outContent = ConvHtmlMarkdown.Convert(ConvMarkdownHtml.SmartTxtConvert(inContent));
                        break;
                    default:
                        Console.WriteLine($"Error: Unsupported conversion direction: {$"{convDirection}"}");
                        return;
                }

                // Write the converted content to the output file
                File.WriteAllText(outPath, outContent);
                Console.WriteLine($"Conversion successful: {inPath} -> {outPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during conversion: {ex.Message}");
            }
        }

        /// <summary>
        /// Detects the encoding of a text file.
        /// This method reads the first 4000 bytes of the file and checks for a Byte Order Mark (BOM).
        /// If no BOM is found, it attempts to validate the content as UTF-8.
        /// If the content is valid UTF-8, it returns UTF-8 encoding; otherwise,
        /// it falls back to Windows-1252 encoding.
        /// <param name="path">The path to the file to check.</param>
        private static Encoding DetectEncoding(string path, Encoding defaultEncoding)
        {
            byte[] bytes = new byte[4000];
            using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                file.Read(bytes, 0, 4000);
            }

            // Check for Byte Order Mark (BOM)
            if (bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf) { return Encoding.UTF8; }
            if (bytes[0] == 0xff && bytes[1] == 0xfe) { return Encoding.Unicode; } // UTF-16 LE
            if (bytes[0] == 0xfe && bytes[1] == 0xff) { return Encoding.BigEndianUnicode; } // UTF-16 BE
            if (bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 0xfe && bytes[3] == 0xff) { return Encoding.UTF32; }
            if (bytes[0] == 0x2b && bytes[1] == 0x2f && bytes[2] == 0x76) { return Encoding.UTF7; }

            Encoding enc = Encoding.ASCII;
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] > 127)
                {
                    enc = Encoding.UTF8;
                    break;
                }
            }
            if (enc == Encoding.ASCII) { return enc; }


            // If there is no BOM, try to validate as UTF-8 without BOM
            if (IsValidUtf8(bytes))
            {
                string text = Encoding.UTF8.GetString(bytes);
                if (text.IndexOf('\uFFFD') == -1) { return Encoding.UTF8; }
            }

            // Fallback
            return defaultEncoding;
        }

        /// <summary>
        /// Checks if the byte array is valid UTF-8.
        /// This method attempts to decode the byte array using UTF-8 encoding.
        /// </summary>
        private static bool IsValidUtf8(byte[] bytes)
        {
            try
            {
                UTF8Encoding utf8 = new UTF8Encoding(false, true); // throw on invalid bytes
                utf8.GetString(bytes);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
