# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),  
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.4.2] - 2025-07-22

### Added
- HTML → Markdown: Support for strikethrough text (~~strikethrough~~) and highlighted text (==highlighted==).
- HTML → Markdown: Support for span elements with class attributes (e.g., `<span class="mark">`).

## [1.4.1] - 2025-07-22

### Added
- HTML → Markdown: Support for links (e.g., `<a href="https://example.com"`).

### Changed
- Rename `Markdown.cs` to `ConvMarkdownHtml.cs` for clarity.
- Rename `HtmlMd.cs` to `ConvHtmlMarkdown.cs` for clarity.
- Rename `AmigaGuide.cs` to `ConvGuideHtml.cs` for clarity.

## [1.4.0] - 2025-07-21

### Added
- 💥 Initial HTML → Markdown converter (HtmlMd.cs)
  - ⚠️ Status: 🧪 Early prototype under testing — will be released in a future version once stable.
  - Supports most core HTML5 elements: headings, paragraphs, bold/italic, blockquotes, unordered lists.
  - Designed to produce clean, standardized Markdown — compatible with Markdown.ToHtml output.
  - Implements basic fallback logic and optional attribute handling.
  - Reversible conversion supported for standard Markdown-generated HTML.

- 📦 New CLI support in mdoc.exe:
  - Example: mdoc input.html output.md.

### Changed
- Minor performance improvements, optimizations in Markdown parsing.
- Pre-allocate space for the HTML output to reduce memory allocations during string concatenation.

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
