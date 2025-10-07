# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [2.3.4] - 2025-10-07

### Fixed

- 🐞 HTML to Markdown: Fixed handling of inline `<code>` tags and `<code>` tags without paragraph tags.

## [2.3.3] - 2025-08-17

### Fixed

- 🐞 Markdown to HTML: Fixed handling multiline blockquotes and multiline ordered/unordered lists.

### Added

## [2.3.2] - 2025-08-14

### Added

- ✅ HTML to Markdown and Plain text to HTML conversion:
  - Added support for multiline lists (lists with wrapped lines).
- ✅ Markdown and Plain text to HTML conversion:
  - Added support for multiline lists (lists with wrapped lines).
  - Added support for basic styling (bold, italic, etc.) in link descriptions (e.g., `[**bold link**](url)`, `[link *italic*](url)`, `[link ~~strikethrough~~](url)`).
- ✅ Added: HTML to plain text conversion.
- ✅ CLI tool mdoc.exe: Added support for:
  - Markdown to plain text conversion.
  - HTML to plain text conversion.
  - AmigaGuide to plain text conversion:
    - Converts AmigaGuide files to readable plain text format (e.g., `mdoc.exe input.guide output.txt`).

### Fixed

- 🐞 Plain text `.txt` to HTML/Markdown conversion: Fixed handling of `/` character.

## [2.3.1] - 2025-08-10

### Added

- MD → HTML conversion: Support for indented code blocks (e.g., indented by 4 spaces or a tab).
- Added: CLI tool mdoc.exe:
  - Plain text `.txt` to Markdown conversion:
  - Automatic detection of file encoding (e.g., UTF-8, ASCII) for `.txt` files.
    - Supports basic formatting, headings, paragraphs, lists, links, pipe tables, and code blocks.
  - Usage: Run `mdoc.exe <input.txt> <output.md>`

### Fixed

- 🐞 HTML → Markdown conversion: Fixed handling character `\n` in paragraphs, headings, and lists.

## [2.3.0] - 2025-08-09

### Added

- 🆕 Smart plain text `.txt` to HTML conversion:
  - Converts plain text files to HTML with basic formatting.
  - Automatic detection of file encoding (e.g., UTF-8, ASCII).
  - Added support uppercase headings (e.g., `       HEADING      ` for H1 or `HEADING` for H2).
  - Supports headings, paragraphs, lists, links, pipe tables, and code blocks.
  - Support for indented code blocks (e.g., indented by 4 spaces or a tab).
  - Usage: `string html = ConvHtmlMarkdown.SmartTxtConvert(txt);`.
- 🆕 CLI tool mdoc.exe:
  - Added smart plain text conversion. Usage: `mdoc.exe input.txt output.html`
  - Added support for `--encoding=<encoding>` command-line option to specify the input file encoding (e.g., `windows-1250`, `utf-8`, `ascii`).
    - If `--encoding` is not specified, the default input encoding for `.txt` and `.guide` files is `windows-1252`. See documentation for the full list of supported encodings.

### Changed

- Improvements to performance.

### Fixed

- Markdown → HTML conversion: Handling of unclosed or mismatched Markdown tags (e.g., `**`, `*`, `~~`, `==`) and fixing them in the HTML output.

## [2.2.2] - 2025-08-06

### Added

- ✅ Markdown → HTML conversion: Added support underlining headings (e.g., `Heading\n===` for H1 or `Heading\n---` for H2).
- ✅ CLI tool mdoc.exe: Added support for `--ignore-warnings` command-line option to ignore warnings during conversion.

## [2.2.1] - 2025-08-05

### Added

- Added parameter `ignoreWarnings = false` to ignore warnings during conversion (`ConvMarkdownHtml.Convert` and `ConvGuideHtml.Convert`).

## [2.2.0] - 2025-08-04

### Added

- ✅ HTML → Markdown conversion:
  - Added method to close improperly closed tags (e.g., `**`, `*`, `==`, `~~`).
  - Added support for additional block elements (e.g., `<div>`).
- ✅ AmigaGuide → HTML conversion:
  - 🚨 Added warnings for syntax issues (e.g., improperly closed tags, unknown commands).
  - Added method to close improperly closed tags (e.g., `@{b}`, `@{i}`, `@{u}`).
  - Added global command `@VER$` for version information
  - Added global command `@(c)` for copyright information
  - Added attribute command `@{plain}` for plain text
  - Added attribute command `@{"Doc" ALink "doc.guide/intro"}` for document links
  - Added attribute command `@{"Doc" System "<command> doc.readme"}` This command executes the AmigaDOS command named in `<command>`.

### Changed

- AmigaGuide → HTML conversion: Improved processing commands `@title`, `@author`

## [2.1.0] - 2025-08-02

### Added

- 🆕 CLI tool mdoc.exe: Added conversion support for **AmigaGuide → Markdown**:
  - Usage: Run `mdoc.exe input.guide output.md` to convert AmigaGuide files to Markdown
  - Note: Some advanced AmigaGuide features may not be fully supported; see documentation for details
- 📂 Source code for CLI tool mdoc.exe is now available in the repository ([src](./src/)).

### Changed

- 🎯 AmigaGuide → HTML conversion improvements:
  - Button styles have been updated for better consistency and appearance. (Replaced `input type="button"` elements with `a href` links styled via CSS.)
    - Improved rendering of `<h2>`, `<pre>`, and `<hr>` elements for better compatibility with Markdown
    - Added support for the commands `@rem`, `@remark`, and `@author`
    - Improved parsing flow

### Fixed

- 🐞 HTML → Markdown conversion: Tags `<b>`, `<i>`, and `<a href...` are now correctly supported within `<pre>` blocks.

## [2.0.3] - 2025-07-28

### Changed

- Improved Markdown → HTML conversion: Readability and formatting of the generated HTML code.

## [2.0.2] - 2025-07-27

### Fixed

- Markdown → HTML conversion: TOC generation now supports documents with skipped heading levels (e.g., `#`, `###` without an intervening `##`).
- Markdown → HTML conversion: Convert improperly started fenced code blocks (e.g., ````csharp`).

## [2.0.1] - 2025-07-26

### Changed

- Improved Markdown → HTML conversion: Enhanced detection of YAML front matter:
  - Added support for meta tags: `version`, `description`, `keywords`, `license`.
  - Enhanced detection of empty front matter blocks and end-of-front-matter marker (`...`).
- Improved Markdown → HTML conversion: Enhanced handling of fenced code blocks.

## [2.0.0] - 2025-07-25

### Added

- 🚨 HTML → Markdown conversion: Added warnings for syntax issues (e.g., improperly closed tags, unknown HTML entities, unexpected characters inside `<pre>` blocks). (2025-07-25)
- ✅ Markdown → HTML conversion: Support for subscript and superscript (e.g., `H~2~O`, `E=mc^2^`).
- ✅ HTML → Markdown conversion support:
  - Support for subscript and superscript (e.g., `<sub>`, `<sup>`).
  - Support for front matter (YAML metadata block).
  - Support for task lists (e.g., `<li><input type="checkbox" checked>Done</li>`). (2025-07-24)
  - Horizontal rules (e.g., `<hr>`, `<hr />`).
  - Links and email links (e.g., `<a href="https://example.com">`, `<a href="mailto:user@example.com">`).
  - Ordered lists.
  - Preformatted text blocks (e.g., `<pre>...</pre>`).
  - Code blocks with language highlighting (e.g., `<pre><code class="csharp">...</code></pre>`).
  - Images with `alt` and `title` attributes (e.g., `<img src="..." alt="..." title="...">`).
  - Tables with alignment (e.g., `| --- | :---: | ---: |`).
  - Strikethrough text (~~strikethrough~~) and highlighted text (==highlighted==).
  - Span elements with class attributes (e.g., `<span class="mark">`).
  - Links (e.g., `<a href="https://example.com"`).
- 🆕 Initial HTML → Markdown converter
  - Supports most core HTML5 elements: headings, paragraphs, bold/italic, blockquotes, unordered lists.
  - Designed to produce clean, standardized Markdown — compatible with Markdown.ToHtml output.
  - Implements basic fallback logic and optional attribute handling.
  - Reversible conversion supported for standard Markdown-generated HTML.
- ✅ New CLI support in mdoc.exe:
  - Example: `mdoc.exe <input.html> <output.md>`

### Changed

- HTML → Markdown conversion:
  - Improved handling of improperly closed paragraph tags.
  - Enhanced handling of `<br>` tags to ensure proper line breaks in Markdown output.
  - Enhanced handling of double space characters in paragraphs to ensure proper line breaks in Markdown output.
- Rename class for clarity:
  - `Markdown.cs` to `ConvMarkdownHtml.cs`
  - `HtmlMd.cs` to `ConvHtmlMarkdown.cs`
  - `AmigaGuide.cs` to `ConvGuideHtml.cs`
- Minor performance improvements, optimizations in Markdown parsing. (2025-07-21)
- Pre-allocate space for the HTML output to reduce memory allocations during string concatenation. (2025-07-21)

## [1.3.0] – 2025-07-19

### Added

- ⚠️ New warning system for malformed Markdown syntax (e.g., unclosed `**`, `*`, `==`).
- 🚨 Warrnings for XSS and phishing attempts (e.g., `<script>`, malformed `<a href>`, etc.).
  - Warnings are collected during parsing and displayed at the end of the HTML output.
  - Default visual styling for warnings via inline `<div>` styling (visible even without external CSS).

### Fixed

- Resolved issue where an unclosed block at the end of a Markdown file could lead to improperly terminated HTML output.
- 🐞 Resolved issue with basic styling across multiline paragraphs (e.g., `**bold**` or `*italic*` spanning multiple lines).

## [1.2.3] – 2025-07-18

### Changed

- TOC HTML code readability improvements (indentation).

### Fixed

- Minor issues with `img` tag attributes (like `title` and `alt`).
- TOC nested `ul` tag rendering, and fixed incorrect rendering of inline styles within TOC entries.

## [1.2.2] – 2025-07-17

### Fixed

- Rendering of inline styles within headings.
- Special character escaping in inline paragraph text.

## [1.2.1] – 2025-07-17

### Added

- 🧪 Optional: Command-line Tool (mdoc.exe) for users who want to try the converter without integrating it into a project.

## [1.2.0] – 2025-07-16

### Added

- 🆕 Support for Table of Contents (TOC) generation.
  - Automatically inserts a hierarchical TOC at the [TOC] marker in the document.

## [1.1.2] – 2025-07-15

### Changed

- ⚡ Improved performance (~4x) of Markdown inline parsing by optimizing autolink and email link detection.

### Added

- Support for line breaks `<br>` if the line ends with a backslash `\`, or if the line contains only a backslash `\`.

## [1.1.1] – 2025-07-12

### Added

- Support for strikethrough text (~~strikethrough~~).
- Support for highlighted text (==highlighted==).

### Fixed

- Resolved issue with improper handling of stacked unordered/ordered lists.

## [1.1.0] – 2025-07-11

### Added

- 🆕 Detection and warning mechanism for potential XSS and phishing attempts.

## [1.0.2] – 2025-07-07

### Added

- Support for plain URLs (`https://example.com`).
- Autolink format (`<https://example.com>`).
- Email links (`user@example.com`, `<user@example.com>`).

## [1.0.1] – 2025-07-06

### Added

- Basic support for inline HTML tags within Markdown.

## [1.0.0] – 2025-07-05

### Added

- Initial release.
- Markdown to HTML conversion.
- AmigaGuide to HTML conversion.
- Support for headings, lists, tables, footnotes, code blocks, links, images, and raw HTML.
- Fully standalone C# implementation with no external dependencies.
