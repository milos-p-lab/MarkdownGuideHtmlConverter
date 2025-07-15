# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),  
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.1.2] – 2025-07-15

### Changed
- Improved performance (~4x) of Markdown inline parsing by optimizing autolink and email link detection.

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
- Detection and warning mechanism for potential XSS and phishing attempts.

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
- Support for headings, lists, tables, footnotes, code blocks, links, images, and raw HTML.
- Fully standalone C# implementation with no external dependencies.
