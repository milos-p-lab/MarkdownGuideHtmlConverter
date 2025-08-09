# Roadmap

This document outlines the planned features, enhancements, and long-term vision for the MarkdownGuideHtmlConverter project.

---

## ✅ Completed

These features are already implemented and functional:

### Markdown to HTML Conversion

- Headings (`#`, `##`, `###`, etc.)
- Heading underlining (e.g., `Heading\n===` for H1 or `Heading\n---` for H2)
- Basic text styles (**bold**, *italic*, ~~strikethrough~~, ==highlighted==)
- Subscript and superscript (e.g., `H~2~O`, `E=mc^2^`)
- Blockquotes
- Inline code and horizontal rules
- Hyperlinks and images (with alt/title)
- Multi-level **ordered lists** and **unordered lists**
- Mixed nesting of **ordered and unordered lists**
- **Task lists** (with checkbox states)
- Tables with column alignment
- Code blocks (with language hints)
- Footnotes with multi-line support and backlinking
- Raw HTML passthrough (audio, video, etc.)
- YAML front-matter → meta tags
- Automatic Table of Contents `[TOC]`
- 🚨 Warnings for syntax and security issues (e.g., unclosed **bold**, *italic*, ==highlight==, etc.)
- 🛡️ Built-in **XSS protection** with sanitization of dangerous content
- 🚨 Warnings for security issues (e.g., embedded &lt;script&gt; tags or suspicious links)
- ✅ The generated HTML code is **valid according to W3C standards**, verified through the [W3C Validator](https://validator.w3.org/).

### HTML to Markdown Conversion

- Headings (`<h1>`–`<h6>`)
- Basic text styles (`<strong>`, `<em>`, `<del>`, `<mark>`)
- Subscript and superscript (e.g., `<sub>`, `<sup>`)
- Span elements with class attributes (e.g., `<span class="lang-en">`)
- Blockquotes
- Ordered lists and unordered lists
- Task lists (with checkbox states)
- Links
- Images with `alt` and `title` attributes (e.g., `<img src="..." alt="..." title="...">`)
- Tables
  - Pipe-style tables with alignment (e.g., `| --- | :---: | ---: |`)
- Preformatted text blocks (e.g., `<pre>...</pre>`)
- Code blocks with language highlighting (e.g., `<pre><code class="language-csharp">...</code></pre>`)
- **Front matter** (YAML metadata block)  
  - Supports title and custom meta tags for HTML `<head>`
- 🚨 Warnings for syntax issues (e.g., improperly closed tags, unknown HTML entities, unexpected characters inside `<pre>` blocks)

### AmigaGuide to HTML Conversion

- Converts the most widely used AmigaGuide commands (rare or advanced commands may not be fully supported):
  - Nodes (`@NODE`, `@ENDNODE`, `@TOC`, `@NEXT`, `@PREV`)
  - Global commands (`@DATABASE`, `@VER$`, `@(C)`, `@TITLE`, `@AUTHOR`)
  - Attribute commands (`@{B}`, `@{I}`, `@{U}`, `@{PLAIN}`, `@{"Doc" LINK "doc.guide/intro"}`, `@{"Doc" SYSTEM "<command> doc.readme"}`)
    - For example, `@{"Doc" LINK "doc.guide/intro"}` creates a link to a node in another document (the `LINK` command specifies the target node or file), and `@{"Doc" SYSTEM "<command> doc.readme"}` opens documents with external commands (the `SYSTEM` command executes a system command, such as opening a file with an external viewer).
    - See [AmigaGuide documentation](https://wiki.amigaos.net/wiki/AmigaGuide_101) for more details on these command forms.
- Preserves the document’s structure for a retro feel
- Generates clean HTML navigation buttons between nodes
- Escapes special HTML characters to safely display content
- 🚨 Warnings for syntax issues (e.g., improperly closed tags, repeated tags, unknown commands, link syntax errors, etc.)
- ✅ The generated HTML code is **valid according to W3C standards**, verified through the [W3C Validator](https://validator.w3.org/).

### Plain Text to HTML Conversion

- Recognizes simple headings (e.g. `Title\n------`)
- Converts bulleted lists (lines starting with `-`, `*`, or `+`)
- Adds paragraph tags and basic inline formatting
- Escapes unsafe characters (`<`, `>`, `&`) automatically
- Outputs valid, minimal, styled HTML — ideal for fast previewing or lightweight rendering of plain notes
- 🔐 It scans the input for potential XSS and phishing vulnerabilities (e.g., embedded &lt;script&gt; tags or suspicious links).

---

## 🚧 In Progress / Planned

Features and improvements currently in development or planned:

- [ ] Optional math / LaTeX support (KaTeX or MathJax integration)
- [x] Definition list syntax (`Term\n: Definition`)
- [ ] Command-line arguments: toggle features (e.g. `--no-toc`, `--unsafe-html`)
- [ ] More robust error reporting (invalid syntax, malformed input)
- [ ] Configurable templates / theming support for output

---

## 💡 Potential Ideas (Exploratory)

Ideas under consideration or awaiting user feedback:

- Export to single-file self-contained `.html` with embedded styles
- Integration with static site generators or CI pipelines
- Markdown extensions (abbr, emoji, custom containers)

---

## 📬 Suggestions?

Have an idea, feature request, or improvement?

Feel free to open an issue or start a discussion here:  
👉 [GitHub Discussions](https://github.com/milos-p-lab/MarkdownGuideHtmlConverter/discussions)

---

Thanks for using and supporting this project!
