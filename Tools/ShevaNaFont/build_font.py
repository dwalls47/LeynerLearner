"""
Builds "Hebrew Sheva Na": Taamey D 0.921 plus a larger sheva glyph for the sounded sheva (sheva na).

The text marks a sounded sheva by putting an invisible joiner right after it -- CGJ (U+034F) or ZWJ
(U+200D) -- and the font's 'ccmp' feature substitutes the large glyph, "sheva.na". Copied text stays
ordinary Unicode, and other fonts simply show an ordinary sheva.

The new glyph is the sheva scaled about its top centre, so it hangs from the same point under the letter.
Every positioning rule of the sheva is extended to it, so it attaches exactly like a sheva.

Taamey D is GPL-2 with a font-embedding exception and Reserved Font Name 'Taamey D' (see FONT-NOTICE.txt),
so the modified font is renamed. Its corresponding source is Taamey D's source package (in this folder)
plus this script.

usage: python build_font.py <Taamey_D.woff2> <expanded Taamey_D.fea> <output .ttf>
  The .fea is produced from the source package's Taamey_D.sym.fea by its main_fill_in_template.py.
"""
import copy
import re
import sys

from fontTools.feaLib.builder import addOpenTypeFeaturesFromString
from fontTools.ttLib import TTFont

SCALE = 1.4
FAMILY = "Hebrew Sheva Na"
PS_NAME = "HebrewShevaNa-Regular"

# ---------------------------------------------------------------------------------------------------
# Feature-file edits: the large sheva goes everywhere the sheva is positioned; the substitution that
# creates it runs first.

NEW_LOOKUP = """
lookup ccmp00ShevaNa {  # sheva + joiner -> sounded sheva (Hebrew Sheva Na)
  sub sheva cgj by sheva.na;
  sub sheva zerojoin by sheva.na;
} ccmp00ShevaNa;
"""

EDITS = [  # (old, new) - each must occur exactly once
    ("@blw_vows = [@wide_vows @xatafs sheva xiriq];",
     "@blw_vows = [@wide_vows @xatafs sheva sheva.na xiriq];"),
    ("markClass [sheva xiriq meteg @wide_vows @xatafs @mics] <anchor 0 0> @A4_bvoms;",
     "markClass [sheva sheva.na xiriq meteg @wide_vows @xatafs @mics] <anchor 0 0> @A4_bvoms;"),
    ("pos meteg' <150 0 0 0> [sheva xiriq];",
     "pos meteg' <150 0 0 0> [sheva sheva.na xiriq];"),
    ("markClass sheva <anchor -10 0> @A45;",
     "markClass [sheva sheva.na] <anchor -10 0> @A45;"),
    ("pos mark [sheva xiriq] <anchor -250 0> mark @A45;",
     "pos mark [sheva sheva.na xiriq] <anchor -250 0> mark @A45;"),
    ("markClass [sheva xiriq] <anchor -70 0> @A71;",
     "markClass [sheva sheva.na xiriq] <anchor -70 0> @A71;"),
    ("feature ccmp {\n", "feature ccmp {\n  lookup ccmp00ShevaNa;\n"),
]


def edit_features(fea: str) -> str:
    fea = fea.replace("\r\n", "\n")
    for old, new in EDITS:
        if fea.count(old) != 1:
            sys.exit(f"feature edit not applicable (found {fea.count(old)} times): {old!r}")
        fea = fea.replace(old, new)
    # The new lookup must be defined before the others: GSUB lookups run in definition order.
    first = re.search(r"^lookup ", fea, re.M)
    return fea[:first.start()] + NEW_LOOKUP.lstrip() + "\n" + fea[first.start():]


# ---------------------------------------------------------------------------------------------------

def add_large_sheva(font: TTFont) -> None:
    glyf, hmtx = font["glyf"], font["hmtx"]
    sheva = glyf["sheva"]
    sheva.recalcBounds(glyf)
    cx, top = (sheva.xMin + sheva.xMax) / 2, sheva.yMax

    big = copy.deepcopy(sheva)
    coords = big.coordinates
    for i, (x, y) in enumerate(coords):
        coords[i] = (round(cx + (x - cx) * SCALE), round(top + (y - top) * SCALE))
    big.recalcBounds(glyf)

    order = font.getGlyphOrder() + ["sheva.na"]
    font.setGlyphOrder(order)
    glyf.glyphs["sheva.na"] = big
    glyf.glyphOrder = order
    hmtx.metrics["sheva.na"] = (0, big.xMin)
    if "post" in font and hasattr(font["post"], "extraNames"):
        font["post"].extraNames.append("sheva.na")


def rename(font: TTFont) -> None:
    name = font["name"]
    for rec in list(name.names):
        if rec.nameID in (1, 3, 4, 6, 16, 17, 21, 22):
            name.names.remove(rec)
    for plat, enc, lang in ((3, 1, 0x409), (1, 0, 0)):
        name.setName(FAMILY, 1, plat, enc, lang)
        name.setName("Regular", 2, plat, enc, lang)
        name.setName(f"{FAMILY}; based on Taamey D 0.921", 3, plat, enc, lang)
        name.setName(FAMILY, 4, plat, enc, lang)
        name.setName(PS_NAME, 6, plat, enc, lang)
        name.setName("Taamey D 0.921 (Ben Denckla, from Taamey Frank CLM by Yoram Gnat), modified for Leyner Learner: "
                     "adds a larger sheva glyph for the sounded sheva (sheva na), selected by sheva + CGJ or ZWJ.",
                     10, plat, enc, lang)


def main(woff2_path: str, fea_path: str, out_path: str) -> None:
    font = TTFont(woff2_path)
    font.flavor = None

    original_classes = dict(font["GDEF"].table.GlyphClassDef.classDefs)
    add_large_sheva(font)

    with open(fea_path, encoding="utf-8") as f:
        fea = edit_features(f.read())
    addOpenTypeFeaturesFromString(font, fea, tables={"GDEF", "GSUB", "GPOS"})

    # feaLib infers glyph classes from the feature file; keep FontForge's classes for any it left out.
    classes = font["GDEF"].table.GlyphClassDef.classDefs
    for g, c in original_classes.items():
        classes.setdefault(g, c)
    classes["sheva.na"] = 3  # mark

    rename(font)
    font.save(out_path)
    print(f"wrote {out_path}: {len(font.getGlyphOrder())} glyphs")


if __name__ == "__main__":
    if len(sys.argv) != 4:
        sys.exit(__doc__)
    main(*sys.argv[1:])
