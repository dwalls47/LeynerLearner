# Frank Ruhl Libre

`FrankRuhlLibre-Regular.ttf` is an **unmodified copy** of the Regular weight of **Frank Ruhl Libre** (version 6.004),
Yanek Iontef's open-source revival of Rafael Frank's classic Hebrew typeface Frank Rühl (1908–1910).

- Source: <https://github.com/fontef/frankruhllibre>, commit `2372d1998e51dc011f86554c0d23f1ccf44afddf`,
  file `fonts/ttf/FrankRuhlLibre-Regular.ttf` (SHA-256
  `20D7B981D876C9DDB0F9109C409B4E495B84F486457F6F220307DE3AFCF70E04`), downloaded 2026-10-27
- Also on Google Fonts: <https://fonts.google.com/specimen/Frank+Ruhl+Libre>
- License: **SIL Open Font License 1.1** (`OFL.txt`, copied from the same commit with `AUTHORS.txt` and
  `CONTRIBUTORS.txt`). It may be bundled and redistributed with software; it may not be sold by itself.
  "Frank Ruhl Libre" is not a Reserved Font Name.

The static TrueType file is used (not the OTF, variable or web versions) because Windows text drawing, the PDF
renderer and System.Drawing all read it, and the app sets Hebrew in a single weight.

## Coverage

It has every Hebrew letter, vowel point, dagesh, shin/sin dot, meteg, rafe, maqaf, paseq and sof pasuq, with
mark positioning (`mark`/`mkmk`). It has **no cantillation (trope) marks** (U+0591–U+05AF), no upper/lower dots
and no inverted nun. With trope in the text, those marks are drawn from another font and may sit out of place,
so it is best for reading without trope, for example practising from a text with vowels only. For the same
style with trope, use Hebrew Sheva Na, whose glyphs come from Taamey Frank CLM (also a Frank Rühl design).

The app loads it for its own use only (it is not installed in Windows) and lists it in the Font list when this
folder is present; the folder is optional in data packs.
