using m.format.conv;
using System;
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
        /// </summary>
        /// <param name="args">Command line arguments: input file path and optional output file path.</param>
        /// <remarks>
        /// If no output file path is provided, the output file will have the same name as the input file,
        /// but with the appropriate extension based on the conversion direction.
        /// </remarks>
        /// <version>2.1.0</version>
        /// <date>2025-08-02</date>
        /// <author>Miloš Perunović</author>
        private static void Main(string[] args)
        {
#if DEBUG
            args = new string[2];
            args[0] = @"R:\Temp\test.html";
            args[1] = @"R:\Temp\test.md";
#endif
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: mdoc <input_file> [output_file]");
                Console.WriteLine("Supported conversions:");
                Console.WriteLine("  .md  -> .html");
                Console.WriteLine("  .html -> .md");
                Console.WriteLine("  .guide -> .html");
                Console.WriteLine("  .guide -> .md");
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
                            outExt = ".html";
                            break;
                        default:
                            outExt = ".unknown";
                            break;
                    }
                    outPath = Path.ChangeExtension(inPath, outExt);
                }

                // Read the input file content
                Encoding enc = inExt == ".guide" ? Encoding.GetEncoding("windows-1250") : Encoding.UTF8;
                string inContent = File.ReadAllText(inPath, enc);

                // Perform conversion based on input and output extensions
                string convDirection = $"{inExt} -> {outExt}";
                switch (convDirection)
                {
                    case ".md -> .html":
                        outContent = ConvMarkdownHtml.Convert(inContent);
                        break;
                    case ".html -> .md":
                        outContent = ConvHtmlMarkdown.Convert(inContent);
                        break;
                    case ".guide -> .html":
                        outContent = ConvGuideHtml.Convert(inContent);
                        break;
                    case ".guide -> .md":
                        outContent = ConvHtmlMarkdown.Convert(ConvGuideHtml.Convert(inContent), markCodeBlock: false);
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
    }
}
