---
title: Document Title
author: Firstname Lastname
date: 2025-08-04
---

# Test Document Title

## Break line

\

Paragraph first line  
second line (v1).

Paragraph first line  
second line (v2).

## Task List

- [x] Done
- [x] In progress
- [ ] Not started

## Code Blocks

``` text
 - Preformatted
 - bold
 - text block
```

``` text
One line
```

Next paragraph

Code Snippet:

``` csharp
int x = 5;
bool ok = x > 0;
```

``` csharp
int x = 6;
bool ok = x > 1;
```

``` text
bool wrong1 = x < 0;
bool wrong2 = x > 0;
```

## Subscripts and Superscripts

Water H~2~O.  
E = mc^2^.
Unsupported tag

## Images

This is an image ![HTML 5 Logo](https://www.w3schools.com/html/html5.gif).

## Ordered, Unordered List

- Item 1
- Item 2
- Item 3

1. Item 1
2. Item 2
3. Item 3

## Quotes and Citations

### Multiple Lines blockquote

> Read the [Markdown](https://en.wikipedia.org/wiki/Markdown) documentation for more information.
>
> This document is written in Markdown format.
>
> Markdown is easy to read and write, and can be easily converted to HTML or other formats.

### Single Line blockquote

> For 50 years, WWF has been protecting the future of nature.

## Basic Text Formatting (**Bold**, *Italic*, ~~Strikethrough~~, ==Highlighted==)

**Bold**, *Italic*, ~~Strikethrough~~, ==Highlighted==

Normal \* **bold** *italic* ***bolditalic*** normal

\*this is not italic\*

\# this is not heading

Nested **bold *italic* bold**

Normal normal **bold  
continues** normal

Normal normal *italic continues* normal

**ADIABATIC PROCESSES** – *Gr. adiabatos – which cannot be crossed*

Normal \* *italic* **bold** ***bolditalic*** normal ~~strikethrough~~ ==highlighted== normal **==highlighted bold==** normal == nohighlighted== \~\~ nostrikethrough\~\~ normal

## Links and Email

[Link example](https://example.com)

[http://google.com](http://google.com)

[user@example.com](mailto:user@example.com)

## Tables

| \<FirstName\> | &LastName | Age |
| --- | :---: | ---: |
| John | Smith | 50 |
| Jane | Doe | 40 |

| Name | Age |
| --- | :---: |
| John | 25 |

---

## Media Embedding

Your browser does not support the video tag.

## Escaped characters

Characters \<, \>, &, and " can be used in Markdown without issues.

& \<\>"€&xxx; are HTML entities.

``` markdown
&amp; &lt; &gt; &quot; &euro; are HTML entities.
```

!"\#\$%&\'()\*+,-./:;\<=\>?@\[\\\]\^\_\`{\|}\~

Unclosed HTML entity: &nbsp

Unknown HTML entity: &wrong;

### Unclosed heading

Unclosed paragraph: Unsclosed paragraph 1

Unclosed paragraph: Unsclosed paragraph 2

**Unclosed bold text**

*Unclosed emphatic text*

----------------------------------------------------

## ⚠️ WARNINGS

- Line 50: Unexpected character `<` inside `<pre>` block.
- Line 51: Unexpected character `>` inside `<pre>` block.
- Line 159: Unknown HTML entity.
- Line 160: Unknown HTML entity: &wrong;.
- Line 161: Improperly closed paragraph/heading tag
- Line 162: Improperly closed paragraph/heading tag
- Line 163: Improperly closed paragraph/heading tag
- Line <= 164: Unclosed bold tag
- Line 164: Improperly closed paragraph/heading tag
- Line <= 165: Unclosed italic tag
