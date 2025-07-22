# Why I Built My Own Markdown to / from HTML Converter

> ✍️ **Author:** Miloš Perunović  
> 🗓️ **Date:** 2025-07-22  
> **Description:** "A lightweight, safe and complete .md to .html, .html to .md, and .guide to .html converter built in C#, and why existing tools like Pandoc and Typora didn't meet my needs."

## Brighter, Smaller, Safer: Why I Built My Own Markdown to / from HTML Converter

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

## 🔍 Why Not Use Pandoc or Typora?

These tools are powerful. But they weren't right for my needs:

| Tool    | Limitations                                                                   |
| ------- | ----------------------------------------------------------------------------- |
| Pandoc  | Heavy binary, slow execution, poor table rendering, no task lists             |
| Typora  | Lacks XSS protection, doesn't support multi-line footnotes, inconsistent HTML |
| VS Code | Basic preview only, no TOC, no customization                                  |

When you care about **structure, safety, and full feature coverage**, even powerful tools fall short.

---

## ✅ Why Build a New One?

I needed a tool that:

- Generates valid HTML5 (passes W3C validation)
- Has built-in XSS protection
- Supports all standard Markdown features — and more:
  - Nested lists
  - Tables with alignment
  - Footnotes with backlinks
  - Raw HTML passthrough (e.g., `audio`, `video`, `br`, etc.)
  - Front matter → HTML `head` meta
  - [TOC] marker for automatic Table of Contents
- Works as a pure C# class, without any dependencies
- Is small enough to embed in any app (desktop, console, or web)

---

## 🛡️ New Bonus Feature: Warnings for Syntax and Security Issues

The converter now includes a built-in warning system:

- 🚨 Detects syntax errors (e.g. unclosed **bold**, *italic*, ==highlight==)
- 🔒 Scans for XSS and phishing attempts (e.g. `<script>`, malformed `<a href>`, etc.)
- 📄 Collects all issues and appends them to the output HTML in a styled warning block
- 📌 Includes line numbers and position info for easier debugging
- 🎯 Uses inline styling so the warning block is always visible — even without external CSS

This makes the tool much safer for batch processing or handling untrusted Markdown sources.

Output is minimalistic, semantic HTML5 — no extra wrappers, no unnecessary classes.

---

## 🧪 How I Use It

This project started as a personal need for clean, fast, embeddable Markdown-to-HTML conversion.

Since then, I’ve used it in:

- 📘 Static website generators
- 🧩 WebView2-based desktop apps
- 🛠️ Batch documentation tools
- 🧪 Testing Markdown parsers and XSS filters

And it continues to evolve.

## 🧠 Design Philosophy

One C# file. One method: Markdown.Convert()

- ⚡ No runtime dependencies
- 🛡️ Safety and standards-compliance by default
- 🔧 Easy to read, fork, modify, extend

If you like this approach, try the .exe version or include the class in your project:

```cmd
mdoc input.md output.html
mdoc input.guide output.html
mdoc input.html output.md
```

Or in C#:

```csharp
string html = ConvMarkdownHtml.Convert(markdown);
```

```csharp
string markdown = ConvHtmlMarkdown.Convert(html);
```

```csharp
string html = ConvGuideHtml.Convert(guide);
```

---

## 💡 What's Next?

Planned features:

- HTML to Markdown conversion (in progress)
- Definition list syntax
- Optional math / LaTeX support

---

## Conclusion

When existing tools don't meet your expectations — build your own.

This converter started as a hobby, but it's now a practical tool I trust in my own workflow. If you're looking for a simple, powerful, and secure Markdown-to-HTML converter, feel free to try it or contribute.

Made with focus and precision by Miloš Perunović 😊

👉 [Try it on GitHub](https://github.com/milos-p-lab/MarkdownGuideHtmlConverter)
