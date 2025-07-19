# Roadmap

This document outlines the planned features, enhancements, and long-term vision for the MarkdownGuideHtmlConverter project.

---

## ✅ Completed

These features are already implemented and functional:

- Headings (h1–h6)
- Bold, italic, strikethrough, highlight, and style combinations
- Ordered, unordered and task lists (with checkbox rendering)
- Code blocks with language detection
- Inline code and horizontal rules
- Blockquotes
- Hyperlinks and images (with alt/title)
- Tables with column alignment
- Footnotes with multi-line support and backlinking
- YAML front matter → converted to HTML meta tags
- Automatic Table of Contents generation from headings
- Raw HTML passthrough (audio, video, etc.)
- Built-in XSS protection with sanitization of dangerous content
- Warnings for syntax and security issues
- Clean, semantic HTML5 output
- Ready-to-use .exe binary (no installation required)

---

## 🚧 In Progress / Planned

Features and improvements currently in development or planned:

- [ ] Optional math / LaTeX support (KaTeX or MathJax integration)
- [x] Definition list syntax (`Term\n: Definition`)
- [ ] Partial HTML-to-Markdown reverse conversion
- [ ] Command-line arguments: toggle features (e.g. `--no-toc`, `--unsafe-html`)
- [ ] More robust error reporting (invalid syntax, malformed input)
- [ ] Configurable templates / theming support for output

---

## 💡 Potential Ideas (Exploratory)

Ideas under consideration or awaiting user feedback:

- Export to single-file self-contained `.html` with embedded styles
- Integration with static site generators or CI pipelines
- Markdown extensions (abbr, emoji, custom containers)
- Option to preserve or strip raw HTML tags
- Optional WYSIWYG mode for previewing/editing before export
- Cross-platform support via .NET 6/8+

---

## 📬 Suggestions?

Have an idea, feature request, or improvement?

Feel free to open an issue or start a discussion here:  
👉 [GitHub Discussions](https://github.com/milos-p-lab/MarkdownGuideHtmlConverter/discussions)

---

Thanks for using and supporting this project! 🙏
