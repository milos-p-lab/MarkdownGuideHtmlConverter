# Why I Built a C# Markdown to/from HTML, and AmigaGuide to HTML Converter

> **Description:** "A lightweight, safe and complete `.md` to `.html`, `.html` to `.md`, `.txt` to `.html`, `.html` to `.txt`, and `.guide` to `.html` converter built in C#, and why existing tools like Pandoc and Typora didn't meet my needs."

## 🚀 Introducing the Markdown to HTML Converter You Didn’t Think Was Possible

> “If you’d told me a year ago that it’s possible to build a faster and fully standards-compliant Markdown-to-HTML converter than Pandoc—in a single C# file, with built-in XSS protection, working on both .NET Framework and .NET 7/8/9—I honestly wouldn’t have believed it myself. So I built it to prove it can be done.”

I’ve always admired tools like Pandoc for their power. But I wanted:

- Blazing fast conversion speed, even for documents hundreds of pages long.
- Smaller footprint, without pulling in hundreds of MB of dependencies.
- Full compatibility across .NET Framework and modern .NET versions.
- W3C-valid HTML output—no broken markup, no surprises.
- Built-in security, to make sure no malicious Markdown can slip through and cause XSS vulnerabilities.
- No external tools, no native binaries, no complex installs.

In my personal and professional work, I needed a reliable Markdown to HTML converter that could:

- Be embedded in **console, desktop, or web applications**
- Produce **clean, W3C-valid HTML5**
- Offer **full feature support**, including task lists, footnotes, tables, TOC, etc.
- Provide **XSS protection** out-of-the-box
- Remain **small, fast, and dependency-free**

After trying several popular tools — Pandoc, Typora, VS Code Markdown preview — I ran into limitations I couldn’t accept.

## 🔍 Why Not Use Pandoc or Typora?

These tools are powerful. But they weren't right for my needs:

| Tool    | Limitations                                                  |
| ------- | ------------------------------------------------------------ |
| Pandoc  | Heavy binary, slow execution, poor table rendering, no task lists |
| Typora  | Lacks XSS protection, doesn't support multi-line footnotes, inconsistent HTML |
| VS Code | Basic preview only, no customization                         |

When you care about **structure, safety, and full feature coverage**, even powerful tools fall short.

As someone who needed a fast, embeddable Markdown to / from HTML converter in **C#**, I couldn't find anything that met all of these goals:

✅ Small and dependency-free  
✅ Fully supports advanced Markdown (TOC, footnotes, tables, tasks...)  
✅ XSS-safe and robust for user input  
✅ Easy to integrate into **console**, **desktop**, or **web apps**

So I built it.

---

## Markdown to HTML Converter

### ✅ Markdown Supported Features

- Headings (`#`, `##`, `###`, etc.)
- Heading underlining (e.g., `Heading\n===` for H1 or `Heading\n---` for H2)
- Basic text styles (**bold**, *italic*, ~~strikethrough~~, ==highlighted==)
- Subscript and superscript (e.g., `H~2~O`, `E=mc^2^`)
- Multi-level **ordered lists** and **unordered lists**
- Mixed nesting of **ordered and unordered lists**
- **Task lists** (with checkbox states)
- Blockquotes
- Code blocks (**code fences**)
  - fenced code blocks (e.g., ```csharp)
  - indented code blocks (e.g., indented by 4 spaces or a tab)
- **Inline code**
- Horizontal rules
- **Links**
- **Images**
- **Tables**  
  - Column alignment (left / center / right)
- **Footnotes**  
  - Clickable references and backlinks
  - Multi-line footnote definitions
  - Inline styles supported inside footnotes
- **Raw HTML** elements  
  - Embedding arbitrary HTML tags inside Markdown
  - Self-closing tags (e.g. `<br>`)
  - Audio/video tags for media embedding
- **Front matter** (YAML metadata block)  
  - Supports title and custom meta tags for HTML `<head>`
- **Table of Contents (TOC)** generation  
  - Automatically collects all headings during parsing
  - Generates hierarchical TOC as nested lists
  - Optionally inserts TOC at `[TOC]` marker in the document

✅ The generated HTML code is **valid according to W3C standards**, verified through the [W3C Validator](https://validator.w3.org/).

### 🔐 Security Considerations

This converter includes built-in logic to detect and sanitize potentially dangerous HTML input:

- Detects and blocks tags such as `<script>`, `<iframe>`, `<object>`, and other potentially unsafe HTML elements.
- Blocks dangerous attributes like `onerror`, `onclick`, `onload`, etc.
- Decodes and analyzes **HTML entity encoding** (e.g. `&#106;...`) and **URL encoding** (e.g. `%6a%61...`) to prevent obfuscated XSS attacks.
- Automatically inserts warnings for any detected issues, allowing users to fix Markdown syntax errors without breaking the conversion process.

No external libraries or HTML sanitizers are required — the security logic is fully self-contained.

### 🚨 Warnings for Syntax and Security Issues

- The converter detects common Markdown syntax mistakes (e.g., unclosed **bold**, *italic*, ==highlight==, etc.).
- It also scans the input for potential XSS and phishing vulnerabilities (e.g., embedded &lt;script&gt; tags or suspicious links).
- Instead of halting the conversion, all issues are collected and displayed as a styled warning block appended to the end of the generated HTML.
- Each warning includes the line number (and optionally the position) where the issue occurred.
- Inline fallback styling is used to ensure visibility of warnings, even without custom CSS.
- This feature improves robustness, especially for automated batch conversions or unverified input sources.

It’s ideal for batch-processing Markdown or handling user-submitted content.

---

## HTML to Markdown Converter

### ✅ HTML Supported Features

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

### 🚨 Warnings for HTML Syntax and Security Issues

- The converter detects common HTML syntax mistakes (e.g., improperly closed tags, unknown HTML entities, unexpected characters inside `<pre>` blocks).
- Instead of halting the conversion, all issues are collected and reported at the end of the HTML output.

---

## AmigaGuide to HTML Converter

### ✅ AmigaGuide Supported Features

- Converts core AmigaGuide commands:
  - Nodes (`@NODE`, `@ENDNODE`, `@TOC`, `@NEXT`, `@PREV`)
  - Global commands (`@DATABASE`, `@VER$`, `@(C)`, `@TITLE`, `@AUTHOR`)
  - Attribute commands (`@{B}`, `@{I}`, `@{U}`, `@{PLAIN}`, `@{"Doc" ALINK "doc.guide/intro"}`, `@{"Doc" SYSTEM "<command> doc.readme"}`)
- Preserves the document’s structure for a retro feel
- Generates clean HTML navigation buttons between nodes
- Escapes special HTML characters to safely display content
- 🚨 Warnings for syntax issues

✅ The generated HTML code is **valid according to W3C standards**, verified through the [W3C Validator](https://validator.w3.org/).

---

## Smart Plain Text to HTML Converter

### ✅ Plain Text Supported Features

- Recognizes simple headings (e.g., `Heading\n===` for H1 or `Heading\n---` for H2) or uppercase headings (e.g., `        HEADING` for H1 or `HEADING` for H2).
- Converts bulleted lists (lines starting with `-`, `*`, `+`, or `•`)
- Adds paragraph tags and basic inline formatting
- Escapes unsafe characters (`<`, `>`, `&`) automatically
- Outputs valid, minimal, styled HTML — ideal for fast previewing or lightweight rendering of plain notes
- 🔐 It scans the input for potential XSS and phishing vulnerabilities (e.g., embedded &lt;script&gt; tags or suspicious links).

---

## HTML to Plain Text Converter

- Headings (e.g., `Heading\n===` for H1 or `Heading\n---` for H2)
- Blockquotes
- Ordered lists, unordered lists and task lists
- Links
- Tables
  - Pipe-style tables with alignment (e.g., `| --- | :---: | ---: |`)
- 🚨 Warnings for syntax issues

---

## Other Derived Conversions

- Markdown to Plain Text
- Smart Plain Text to Markdown
- AmigaGuide to Markdown
- AmigaGuide to Plain Text

---

## ⚡ One C# File. One Line to Use It

Instead of building a framework, I created a **single-file class** you can just drop into your project and use like this:

```csharp
string html = ConvMarkdownHtml.Convert(markdown);
```

```csharp
string markdown = ConvHtmlMarkdown.Convert(html);
```

```csharp
string html = ConvGuideHtml.Convert(amigaGuide);
```

```csharp
string html = ConvHtmlMarkdown.SmartTxtConvert(txt);
```

```csharp
string txt = ConvHtmlMarkdown.ConvertToTxt(html);
```

Done. No NuGet packages. No third-party libs. No surprises.

---

## 🌟 Additional Benefits

- Fast conversion (e.g. a ~100-page book converts in just a few tens of milliseconds on a standard PC)
- Compatible with both .NET Framework 4.x and .NET 7/8/9
- Minimal footprint (just a few tens of KB)
- Supports custom CSS themes for beautiful HTML rendering
- No dependencies on external DLLs or tools like Pandoc
- 🛡️ **Built-in XSS protection** — automatically detects dangerous tags, attributes, and obfuscated payloads for safer HTML output

---

## 🧠 Design Philosophy

One C# file. One method.

- ⚡ No runtime dependencies
- 🛡️ Safety and standards-compliance by default
- 🔧 Easy to read, fork, modify, extend

---

## 📦 Try It or Fork It

🔹 Just want to test it? Download the [mdoc.exe](https://github.com/milos-p-lab/MarkdownGuideHtmlConverter/tree/main/bin) and run:

```cmd
mdoc input.md output.html
mdoc input.md output.txt
mdoc input.html output.md
mdoc input.html output.txt
mdoc input.txt output.html
mdoc input.txt output.md
mdoc input.guide output.html
mdoc input.guide output.md
mdoc input.guide output.txt
```

🔹 Want to embed or extend it? Just copy the `.cs` file into your project and you're done.

> 👉 GitHub: [milos-p-lab/MarkdownGuideHtmlConverter](https://github.com/milos-p-lab/MarkdownGuideHtmlConverter/tree/main/src)

---

If you're tired of bloated or unsafe Markdown tools — try this minimalist approach. I built it for me, but maybe it's exactly what you need too.

---

Made with focus and precision by Miloš Perunović 😊
