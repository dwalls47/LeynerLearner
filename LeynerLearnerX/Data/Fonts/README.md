# TropeTrainer Hebrew font

`TropeTrainerHebrew.ttf` is **Taamey D 0.921** (Ben Denckla; glyphs from Taamey Frank CLM by Yoram Gnat)
with one addition: a larger sheva glyph for the **sounded sheva** (sheva na), used by the app's
"Sounded sheva: Large sheva" option.

- The text marks a sounded sheva with U+034F COMBINING GRAPHEME JOINER right after it (sheva + ZWJ works
  too). The font's `ccmp` feature replaces that pair with the large glyph; other fonts just show an ordinary
  sheva, and copied text is ordinary Unicode.
- The large glyph is the sheva scaled 1.4× about its top centre, and every positioning rule of the sheva
  applies to it, so it attaches under the letter exactly like a sheva.

## License

GNU GPL version 2 with a font-embedding exception, and Reserved Font Name "Taamey D" — see
`FONT-NOTICE.txt` and `GPL-2.0.txt`. Because the font is modified, it is renamed. Under the exception,
PDFs that embed it are not themselves covered by the GPL.

**Corresponding source:** `Tools\ShevaNaFont\` in this repository holds Taamey D's unmodified source
package (`Taamey_D-0.921-source-40115a364a4e.zip`, from
<https://github.com/bdenckla/MAM-basics>, `in/font-support/taamey-d-0.921/`) and `build_font.py`, which
produces this file from the published `Taamey_D.woff2` (SHA-256
`5cc8df8ae3311b91e506edbb294561f6f0e39ebe4260bdb972c90902186c2474`). Anyone distributing the app must
make that source available along with the font.
