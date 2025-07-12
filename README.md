# Markdown & AmigaGuide to HTML Converter

⚡ **Fast .NET converter for Markdown (.md) and AmigaGuide (.guide) files to HTML.**

This project contains two simple yet powerful C# classes that convert Markdown and AmigaGuide documents into fully valid HTML. The converters are developed for speed, small footprint, and independence from external libraries, making them perfect for integration into console, desktop and web applications.

---

## 📄 C# Markdown to HTML Converter

> ✍️ **Author:** Miloš Perunović  
> 🗓️ **Date:** 2025-07-11

### 🚀 Introducing the Markdown to HTML Converter You Didn’t Think Was Possible

> “If you’d told me a year ago that it’s possible to build a faster and fully standards-compliant Markdown-to-HTML converter than Pandoc—in a single C# file, with built-in XSS protection, working on both .NET Framework and .NET 7/8/9—I honestly wouldn’t have believed it myself. So I built it to prove it can be done.”

I’ve always admired tools like Pandoc for their power. But I wanted:
- Blazing fast conversion speed, even for documents hundreds of pages long.
- Smaller footprint, without pulling in hundreds of MB of dependencies.
- Full compatibility across .NET Framework and modern .NET versions.
- W3C-valid HTML output—no broken markup, no surprises.
- Built-in security, to make sure no malicious Markdown can slip through and cause XSS vulnerabilities.
- No external tools, no native binaries, no complex installs.

### ✅ Supported Features

- Headings (**h1**, **h2**, **h3**, **h4**, **h5**, **h6**)
- Basic text styles (**bold**, *italic*, ***bolditalic***)
- Strikethrough (~~strikethrough~~)
- Highlighting (==highlighted==)
- Multi-level **ordered lists**
- Multi-level **unordered lists**
- Mixed nesting of **ordered and unordered lists**
- **Task lists** (with checkbox states)
- Blockquotes
- Code blocks (**code fences**)
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
- Automatic HTML escaping for `<`, `>`, `&`, etc.
- **Front matter** (YAML metadata block)  
  - Supports title and custom meta tags for HTML `<head>`

✅ The generated HTML code is **valid according to W3C standards**, verified through the [W3C Validator](https://validator.w3.org/).

### ⚙️ Additional Benefits

- Fast conversion (e.g. a ~100-page book converts in milliseconds on a standard PC)
- Compatible with both .NET Framework and .NET 7/8/9
- Minimal footprint (just a few tens of KB)
- Supports custom CSS themes for beautiful HTML rendering
- No dependencies on external DLLs or tools like Pandoc
- 🛡️ **Built-in XSS protection** — automatically detects dangerous tags, attributes, and obfuscated payloads for safer HTML output

### 🔐 Security Considerations

This converter includes built-in logic to detect and sanitize potentially dangerous HTML input:

- Detects and blocks tags such as `<script>`, `<iframe>`, `<object>`, and other potentially unsafe HTML elements.
- Blocks dangerous attributes like `onerror`, `onclick`, `onload`, etc.
- Decodes and analyzes **HTML entity encoding** (e.g. `&#106;...`) and **URL encoding** (e.g. `%6a%61...`) to prevent obfuscated XSS attacks.
- Automatically escapes or rejects unsafe input during conversion, ensuring that even cleverly encoded payloads cannot slip through unnoticed.

No external libraries or HTML sanitizers are required — the security logic is fully self-contained and works in both .NET Framework 4.x and modern .NET versions.

### ⚠️ Limitations

Note about CommonMark Compliance

This converter implements Markdown-to-HTML conversion in a way compatible with most commonly used Markdown syntax. However, it is not a strict implementation of the official CommonMark specification.

Instead, it:
- uses modern HTML5 output (e.g. &lt;hr&gt; instead of &lt;hr /&gt;)
- escapes potentially dangerous tags and attributes for XSS protection
- indents HTML output for readability
- automatically injects warnings if it detects syntax errors in the Markdown input

For most real-world documents, the converter produces results very similar to CommonMark parsers, but there may be differences in certain edge cases, especially:
- whitespace handling around block elements
- mixed nested lists with unusual indentation
- some less frequently used syntax from the CommonMark test suite

If your project requires strict CommonMark compliance or identical output for all test cases, you might want to use a specialized library like CommonMark.NET.

Otherwise, this converter aims to balance speed, HTML correctness, security, and practical features beyond the scope of the CommonMark specification.

---

## 📄 C# AmigaGuide to HTML Converter

This converter enables viewing **.guide documents** (AmigaGuide format) directly in Windows or web applications without requiring external tools. It’s perfect for retro projects or preserving old Amiga documentation in modern formats.

### ✅ Supported Features

- Converts core AmigaGuide commands:
  - nodes (`@NODE`, `@ENDNODE`)
  - navigation links (`@TOC`, `@NEXT`, `@PREV`)
  - basic text styles (`@{b}`, `@{i}`, `@{u}`)
- Preserves the document’s structure for a retro feel
- Generates clean HTML navigation buttons between nodes
- Escapes special HTML characters to safely display content

---

## 🛠 Installation

No installation required — these are pure C# classes that you can simply add to your .NET project.

---

## 📝 Usage

Example usage for Markdown:

```csharp
string mdContent = File.ReadAllText("document.md");
string html = Markdown.ToHtml(mdContent);
```

## 💡 Why?

I developed these converters because:
- I wanted fast document conversion for my applications
- I didn’t want to depend on large tools like Pandoc
- I love the retro world of Amiga and wanted native support for .guide files

## 💬 Contributing

- Contributions are very welcome!
- If you have ideas for new features or optimizations, please open an Issue.
- If you’d like to improve the code, feel free to create a Pull Request.
- Share any Markdown or AmigaGuide documents that could help with further testing and improvements.

## 📄 License

MIT License – © 2025 Miloš Perunović
