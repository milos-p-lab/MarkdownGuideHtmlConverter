---
title: "MarkdownGuideHtmlConverter"
layout: default
description: "Fast, safe and feature-complete Markdown to HTML converter written in C#."
---

# MarkdownGuideHtmlConverter

**A lightweight, reliable, and secure Markdown to HTML converter built in C#.**

GitHub repository: [milos-p-lab/MarkdownGuideHtmlConverter](https://github.com/milos-p-lab/MarkdownGuideHtmlConverter)

---

## 🔧 What It Does

- Converts Markdown `.md` files to valid HTML5
- Fully supports:
  - Headings, bold/italic/strikethrough/highlight
  - Ordered, unordered, and task lists
  - Tables with alignment
  - Code blocks (with language hints)
  - Footnotes with backlinking
  - Inline HTML passthrough
  - YAML front-matter → meta tags
  - Automatic Table of Contents `[TOC]`
- Built-in 🛡️ **XSS protection**
- Produces **clean, semantic HTML** — easy to embed anywhere

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

```bash
mdcoc.exe input.md output.html
```

Works on Windows with .NET Framework 4.0+

---

## 📌 Roadmap & Contribution

Interested in what's next? 👉 [See the Roadmap](../ROADMAP.md)

Have ideas or want to contribute? 👉 [Open an issue or discussion](https://github.com/milos-p-lab/MarkdownGuideHtmlConverter/discussions)

---

Made with focus and precision by Miloš Perunović 😊
