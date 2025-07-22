---
title: "MarkdownGuideHtmlConverter"
layout: default
description: "Fast, safe and feature-complete Markdown ⇄ HTML converter written in C#."
---

# MarkdownGuideHtmlConverter

**A lightweight, reliable, and secure Markdown ⇄ HTML converter built in C#.**

GitHub repository: [milos-p-lab/MarkdownGuideHtmlConverter](https://github.com/milos-p-lab/MarkdownGuideHtmlConverter)

---

## 🔧 What It Does

### Converts Markdown `.md` files to valid HTML5 `.html`

- Fully supports:
  - Headings, bold/italic/strikethrough/highlight
  - Ordered, unordered, and task lists
  - Tables with alignment
  - Code blocks (with language hints)
  - Footnotes with backlinking
  - Inline HTML passthrough
  - YAML front-matter → meta tags
  - Automatic Table of Contents `[TOC]`
- 🛡️ Built-in **XSS protection**
- 🚨 Warnings for syntax and security issues
- ✅ Produces **clean, semantic HTML** — easy to embed anywhere

### Converts HTML `.html` to Markdown `.md`

- Fully supports:
  - Headings (h1–h6)
  - Bold, italic, and style combinations
  - Blockquotes
  - Unordered lists
  - ⚠️ Status: Early prototype under testing

### Converts Amino Guide `.guide` files to HTML `.html`

- Converts core AmigaGuide commands:
  - nodes (`@NODE`, `@ENDNODE`)
  - navigation links (`@TOC`, `@NEXT`, `@PREV`)
  - basic text styles (`@{b}`, `@{i}`, `@{u}`)
- Preserves the document’s structure for a retro feel
- Generates clean HTML navigation buttons between nodes
- Escapes special HTML characters to safely display content

---

## 💡 Key Design Goal

This library was built with one major principle in mind:

> **Minimal, dependency-free, plug-and-play Markdown → HTML conversion.**

It is designed to be seamlessly integrated into:

- Console applications
- Desktop applications (WinForms, WPF)
- Web applications (ASP.NET, WebView2-based, etc.)

Just copy the C# class into your project and use it directly:

```csharp
string html = Markdown.ToHtml(mdContent);
```

No runtime dependencies, no configuration, no surprises.

---

## ✨ Why Not Use Pandoc or Typora?

While powerful, they come with limitations:

- Pandoc is heavy, slow, and lacks full support for task lists & TOC.
- Typora lacks XSS filtering and proper footnote handling.
- VS Code preview is too basic.

For real-world apps — performance, control, and safety matter.

Read the full story here: 👉 [Why I Built This Converter](blog.md)

---

## ▶️ Try It Now

No setup needed — just download and run:

```cmd
mdoc input.md output.html
mdoc input.html output.md
mdoc input.guide output.html
```

Works on Windows with .NET Framework 4.0+

---

## 📌 Roadmap & Contribution

Interested in what's next? 👉 [See the Roadmap](ROADMAP.md)

Have ideas or want to contribute? 👉 [Open an issue or discussion](https://github.com/milos-p-lab/MarkdownGuideHtmlConverter/discussions)

---

## 🌐 Help Others Discover It

This converter was built to fill the real gaps left by many existing tools:

- More complete feature support
- XSS-safe HTML output
- Zero dependency usage
- Fast execution and clean output

If you find it useful, consider starring the GitHub repo ⭐ and sharing the [blog post](blog.md) that explains why it was created.

This helps others discover a safer and more powerful alternative to bloated or limited Markdown tools.

---

Made with focus and precision by Miloš Perunović 😊
