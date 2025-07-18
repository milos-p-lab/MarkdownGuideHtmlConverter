# Why I Built My Own Markdown to HTML Converter

> ✍️ **Author:** Miloš Perunović  
> 🗓️ **Date:** 2025-07-18  
> **Description:** "A lightweight, safe and complete .md to .html converter built in C#, and why existing tools like Pandoc and Typora didn't meet my needs."

## Brighter, Smaller, Safer: Why I Built My Own Markdown to HTML Converter

Markdown is great. HTML is everywhere. Turning one into the other *should* be easy, right?

Well — not always.

In my personal and professional work, I needed a reliable Markdown to HTML converter that could:

- Be embedded in **console, desktop, or web applications**
- Produce **clean, W3C-valid HTML5**
- Offer **full feature support**, including task lists, footnotes, tables, TOC, etc.
- Provide **XSS protection** out-of-the-box
- Remain **small, fast, and dependency-free**

After trying several popular tools — Pandoc, Typora, VS Code Markdown preview — I ran into limitations I couldn’t accept.

So I built my own.

---

## Why Not Use Pandoc or Typora?

These tools are powerful. But they weren't right for my needs:

| Tool    | Limitations                                                                   |
| ------- | ----------------------------------------------------------------------------- |
| Pandoc  | Heavy binary, slow execution, poor table rendering, no task lists             |
| Typora  | Lacks XSS protection, doesn't support multi-line footnotes, inconsistent HTML |
| VS Code | Basic preview only, no TOC, no customization                                  |

When you care about **structure, safety, and full feature coverage**, even powerful tools fall short.

---

## What My Converter Supports

Built in C#, my converter already supports:

- Headings (`#` to `######`)
- **Bold**, *italic*, ~~strikethrough~~, ==highlight==
- Combined styles (e.g. `**==highlighted bold==**`)
- Nested **ordered/unordered lists**
- **Task lists** with checkbox states (`- [x]`, `- [ ]`)
- **Blockquotes** and inline quotes
- **Code blocks** with language hinting
- **Inline code**
- **Horizontal rules**
- **Hyperlinks** and images (with alt/title)
- **Tables** with column alignment
- **Footnotes** (multi-line, styled, with backlinks)
- **Raw HTML passthrough** (`<video>`, `<br>`, etc.)
- **YAML front matter** → converted into HTML `<head>` tags
- **Automatic Table of Contents** via `[TOC]` marker
- 🛡️ **XSS protection** — dangerous tags/attributes are detected and sanitized

Output is minimalistic, semantic HTML5 — no extra wrappers, no unnecessary classes.

---

## Example

Markdown:

```md
- [x] Done
- [ ] In progress

[^1]: This is a multi-line footnote.  
      It supports **inline styles** and proper backlinking.

[link](https://example.com)
```

HTML:

```html
<ul>
  <li><input type="checkbox" checked> Done</li>
  <li><input type="checkbox"> In progress</li>
</ul>
<p><sup><a href="#fn1" id="ref1">1</a></sup></p>
<div class="footnotes">
  <ul>
    <li id="fn1">This is a multi-line footnote.<br>
    It supports <strong>inline styles</strong> and proper backlinking. <a href="#ref1" class="footnote-backref">↩</a></li>
  </ul>
</div>
```

---

## How to Use It

The converter is available as a simple executable — no installation, no setup.

GitHub: [milos-p-lab/MarkdownGuideHtmlConverter](https://github.com/milos-p-lab/MarkdownGuideHtmlConverter)

Supports: `.NET Framework 4.0+`, Windows systems (XP, Vista, 7, 8, 10, 11).

Example usage:

```bash
mdoc.exe input.md output.html
```

---

## What's Next?

Planned features:

- Optional math / LaTeX support
- Definition list syntax
- HTML-to-Markdown (partial) reverse support
- Web-based live preview mode

---

## Conclusion

When existing tools don't meet your expectations — build your own.

This converter started as a hobby, but it's now a practical tool I trust in my own workflow. If you're looking for a simple, powerful, and secure Markdown-to-HTML converter, feel free to try it or contribute.

👉 [Try it on GitHub](https://github.com/milos-p-lab/MarkdownGuideHtmlConverter)
