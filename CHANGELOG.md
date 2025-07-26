# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),  
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [2.0.1] - 2025-07-26

### Changed
- Improved MD → HTML conversion: Enhanced detection of YAML front matter:
  - Added support for meta tags: `version`, `description`, `keywords`, `license`.
  - Enhanced detection of empty front matter blocks and end-of-front-matter marker (`...`).
- Improved MD → HTML conversion: Enhanced handling of fenced code blocks.

## [2.0.0] - 2025-07-25

### Added
- 🚨 HTML → MD conversion: Added warnings for syntax issues (e.g., improperly closed tags, unknown HTML entities, unexpected characters inside `<pre>` blocks). (2025-07-25)
- 🆕 MD → HTML conversion: Support for subscript and superscript (e.g., `H~2~O`, `E=mc^2^`).
- 🆕 HTML → MD conversion:
  - Support for subscript and superscript (e.g., `<sub>`, `<sup>`).
  - Support for front matter (YAML metadata block).
- 🆕 HTML → MD conversion: Support for task lists (e.g., `<li><input type="checkbox" checked>Done</li>`). (2025-07-24)
- 🆕 HTML → MD conversion support: (2025-07-23)
  - Horizontal rules (e.g., `<hr>`, `<hr />`).
  - Links and email links (e.g., `<a href="https://example.com">`, `<a href="mailto:user@example.com">`).
  - Ordered lists.
  - Preformatted text blocks (e.g., `<pre>...</pre>`).
  - Code blocks with language highlighting (e.g., `<pre><code class="csharp">...</code></pre>`).
  - Images with `alt` and `title` attributes (e.g., `<img src="..." alt="..." title="...">`).
  - Tables with alignment (e.g., `| --- | :---: | ---: |`).
- HTML → MD conversion support: [2025-07-22]
  - Strikethrough text (~~strikethrough~~) and highlighted text (==highlighted==).
  - Span elements with class attributes (e.g., `<span class="mark">`).
  - Links (e.g., `<a href="https://example.com"`).
- 💥 Initial HTML → MD converter (2025-07-21)
  - Supports most core HTML5 elements: headings, paragraphs, bold/italic, blockquotes, unordered lists.
  - Designed to produce clean, standardized Markdown — compatible with Markdown.ToHtml output.
  - Implements basic fallback logic and optional attribute handling.
  - Reversible conversion supported for standard Markdown-generated HTML.
- 📦 New CLI support in mdoc.exe:
  - Example: mdoc input.html output.md.

### Changed
- HTML → MD conversion: (2025-07-24)
  - Improved handling of improperly closed paragraph tags.
  - Enhanced handling of `<br>` tags to ensure proper line breaks in Markdown output.
  - Enhanced handling of double space characters in paragraphs to ensure proper line breaks in Markdown output.
- Rename class for clarity (2025-07-22)
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
- 🐛 Resolved issue with basic styling across multiline paragraphs (e.g., `**bold**` or `*italic*` spanning multiple lines).

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
