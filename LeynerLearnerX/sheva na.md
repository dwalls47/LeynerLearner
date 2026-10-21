Probably yes, and the MAM project may already have built such a font. Its Phonetic MAM pipeline marks vocal shevas with U+05C8 (an unassigned code point just past the Hebrew marks), and the repository bundles a modified "Taamey D" font for its display pages. I'll check whether that font draws a glyph for it:

I'll search the Taamey D source files (SFD outline and OpenType features) for a glyph mapped to U+05C8 or a private-use area codepoint.

Yes, it's feasible, but it means building a modified font. No existing font I can find has a glyph for this:
- **Unicode** has no code point for sheva na.
- **Taamey D**, the font MAM itself uses (derived from Taamey Frank CLM), has only an ordinary sheva and a few contextual variants. I checked its source: nothing at U+05C8 or in the private-use area (PUA).

## What it would take
1. **A font with a new glyph:** a sheva-na mark at a PUA code point (e.g. U+E001). The glyph is the easy part. The real work is making it behave like a sheva: zero width, classed as a mark, and included in every positioning and substitution rule the font applies to the ordinary sheva (attaching to each letter, stacking with dagesh, the final-kaf ligature, and so on). Taamey D is the natural base: it's GPL-2 with an embedding exception, its sources are published, and its rules already follow MAM's mark order. Renaming it would be required.
2. **App changes, which are small:** the app already knows every sounded sheva. Swapping U+05B0 for the PUA code at those positions is a few lines, and the PDF can load the bundled font without installing it.

## The catches
- **On-screen display is the risky part.** The PDF and printing go through HarfBuzz, which positions a glyph using the font's own data. That's likely to work even though Unicode doesn't treat a PUA character as a combining mark. The on-screen RichTextBox uses Windows' own text engine (Uniscribe), which may split the PUA character into a separate run and drop the mark-positioning, leaving the mark misplaced. The fallback would be to show pages as images on screen, as the tikkun already does.
- **It locks you to one font.** Any other font would show an empty box, so the app would have to fall back to coloring for fonts without the glyph.
- **Copy and search break.** Text copied from the PDF would contain the PUA code instead of a real sheva.
- **A cleaner alternative:** keep the real sheva (U+05B0) and put an invisible joiner after it. The font then swaps in the sheva-na glyph through a contextual substitution rule. Copied text stays normal Hebrew, and Windows' text engine is more likely to handle it. Taamey D already uses one such character (CGJ) for another purpose, so it would need a different one, such as ZWJ, and each text engine handles those a little differently. I'd test both approaches.
- **Tools and design:** building a font needs FontForge or Python's fontTools, neither of which is on this machine, so I'd need your go-ahead to download one. You'd also need to choose how the mark should look, for example a slightly larger sheva, or a sheva with a small bar.

## My recommendation
For printed tikkunim and handouts, it's worth trying. I'd test the joiner approach first because it degrades gracefully, with the PUA character as the fallback. I'd keep the current color marking as the default and for any font that lacks the glyph.

If you want to go ahead, tell me which mark design you'd like and whether I may install FontForge or fontTools, and I'll build a test font and check it in both the PDF and the on-screen view.