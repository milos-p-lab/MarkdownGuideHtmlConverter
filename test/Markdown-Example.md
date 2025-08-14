---
title: My Document Title
author: Miloš Perunović
description: This is a sample Markdown document to demonstrate various Markdown features.
keywords: markdown, sample, features, text formatting
date: 2025-08-06
---

Table of Contents
-----------------

[TOC]

## Basic Text Formatting (**Bold**, *Italic*, ~~Strikethrough~~, ==Highlighted==)

Normal   normal **bold  
continues** normal

Normal normal *italic
continues* normal

Normal \* **bold** *italic* ***bolditalic*** normal ~~strikethrough~~ ==highlighted== normal **==highlighted bold==** normal == nohighlighted== ~~ nostrikethrough~~ normal

Nested **bold *italic* bold**

\*this is not italic\*

\# this is not heading

## Images, Links, and Email

This is an image ![HTML 5 Logo](https://www.w3schools.com/html/html5.gif "HTML 5 Logo").

[link](https://example.com)

http://google.com

<http://google.com>

user@example.com

<user@example.com>

Water H<sub>2</sub>O.<br>E = mc<sup>2</sup>.

Water H~2~O.  
E = mc^2^.

x = 2 ^ 2

x^

This is some text with a footnote.[^1] user@example.com

---

## Media Embedding

<video width="320" height="240" controls>
  <source src="https://www.w3schools.com/tags/movie.mp4" type="video/mp4">
  Your browser does not support the video tag.
</video>

## Task List

- [x] Done
- [ ] In progress
- [ ] Not started

## Tables

| First Name | Last Name | Age |
|:-----------|:---------:|----:|
| John       | Smith     | 50  |
| Jane       | Doe       | 40  |

Here goes more text with a second footnote.[^note2]

[^note2]: This is the **second** footnote.

## Code Blocks

``` csharp
int x = 5;
bool ok = x > 0;
```

Code blocks can also be written like this:

    10 print "Hello, World!"
    20 goto 10

## Escaped characters

Characters <, >, &, and " can be used in Markdown without issues.

&amp; &lt; &gt; &quot; &euro; &xxx; are HTML entities.

``` markdown
&amp; &lt; &gt; &quot; &euro; are HTML entities.
```

\!\"\#\$\%\&\'\(\)\*\+\,\-\.\/\:\;\<\=\>\?\@\[\\\]\^\_\`\{\|\}\~

Paragraph first line  
second line (v1).

Paragraph first line\
second line (v2).

## Quotes and Citations

> Read the [Markdown](https://en.wikipedia.org/wiki/Markdown) documentation for more information.
>
> This document is written in Markdown format.
> Markdown is easy to read and write, and can be easily converted to HTML or other formats.

## Lists

- aaa
  - qqq
  - www
    - eee
- bbb

- Item 1
- Item 2
  1. Nested ordered
  2. Another item
    - Nested unordered

Text...

- Item 3

1. aaa
2. bbb
3. ccc
   1. ddd

## Warnings test

Unclosed **bold

Unclosed *italic

Incorrectly written character for ** bold

Incorrectly written character for * italic

[^1]: This is > text of the **first** footnote. [link](https://example.com)
      This is the second line of the **first** footnote.
