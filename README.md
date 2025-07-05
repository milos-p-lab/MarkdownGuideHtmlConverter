# Markdown & AmigaGuide to HTML Converter

⚡ **Fast .NET converter for Markdown (.md) and AmigaGuide (.guide) files to HTML.**

This project contains two simple yet powerful C# classes that convert Markdown and AmigaGuide documents into fully valid HTML. The converters are developed for speed, small footprint, and independence from external libraries, making them perfect for integration into console, desktop and web applications.

---

## 📄 C# Markdown to HTML Converter

> ✍️ **Author:** Miloš Perunović  
> 🗓️ **Date:** 2025-07-04

### ✅ Supported Features

- Headings (**h1**, **h2**, **h3**, **h4**, **h5**, **h6**)
- Basic text styles (**bold**, *italic*, ~~strikethrough~~)
- Multi-level **ordered lists**
- Multi-level **unordered lists**
- Mixed nesting of **ordered and unordered lists**
- **Task lists**
- Blockquotes
- Code blocks (**code fences**)
- **Inline code**
- Horizontal rules
- **Links**
- **Images**
- **Tables**
- **Footnotes**
- **Raw HTML** elements — supports embedding HTML tags directly within Markdown text

✅ The generated HTML code is **valid according to W3C standards**, verified through the [W3C Validator](https://validator.w3.org/).

### ⚙️ Additional Benefits

- Fast conversion (e.g. a ~100-page book converts in milliseconds on a standard PC)
- Compatible with both .NET Framework and .NET 7/8/9
- Minimal footprint (just a few tens of KB)
- Supports custom CSS themes for beautiful HTML rendering
- No dependencies on external DLLs or tools like Pandoc

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
