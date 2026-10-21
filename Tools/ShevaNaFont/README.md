# Building Hebrew Sheva Na

`build_font.py` makes `LeynerLearner\Data\Fonts\HebrewShevaNa.ttf` from Taamey D 0.921 by adding a
larger sheva glyph for the sounded sheva (see that folder's README).

Requirements: Python 3 with `fonttools` and `brotli` (`pip install fonttools brotli`).

1. Get Taamey D's published font, `Taamey_D.woff2`, from MAM-basics commit
   `202e69e8e063438b5faca0d73d20a5cde3d4fb61`, path `doc/woff2/Taamey_D.woff2`
   (SHA-256 `5cc8df8ae3311b91e506edbb294561f6f0e39ebe4260bdb972c90902186c2474`).
2. Unzip `Taamey_D-0.921-source-40115a364a4e.zip` (this folder; Taamey D's unmodified source package) and
   expand its feature-file template from the `sources` folder:

       python main_fill_in_template.py Taamey_D .\font-m-TaD\Taamey_D.sym.fea .\font-m-TaD\Taamey_D.fea

3. Build:

       python build_font.py Taamey_D.woff2 <path>\Taamey_D.fea ..\..\LeynerLearner\Data\Fonts\HebrewShevaNa.ttf

The source package and the GPL text are kept here as the font's corresponding source (GPL-2).
