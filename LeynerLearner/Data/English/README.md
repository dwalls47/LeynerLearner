# English translation

The English text in this folder is derived from the **World Messianic Bible** (WMB, also called the Hebrew Names
Version or the World English Bible: Messianic Edition), "August 2022 stable text", by Michael Paul Johnson and
eBible.org.

- Source: <https://eBible.org/find/details.php?id=engwmb>, file `engwmb_vpl.zip` (verse-per-line text),
  downloaded 2026-10-20, SHA-256 `18A7519B8BF6A749E7B021281705214066D0B7D290998B589CDB2BCB7474900A`
- License: **public domain**. eBible.org's statement: "The World Messianic Bible is not copyrighted, however
  'World Messianic Bible' is a Trademark." It asks that a changed text not be called the World Messianic Bible.
  These files are changed (see below), so the app calls them "English, based on the World Messianic Bible".

## How the files were made

`Tools\EnglishGenerator` reads `engwmb_vpl.txt` and writes one file per book (`Gen.txt`, `Exod.txt`, ...), one
verse per line as `chapter:verse text`, UTF-8. It makes two kinds of change:

1. **Jewish verse numbering.** The WMB follows the chapter and verse numbering of English (Christian) Bibles,
   which differs from the Hebrew in places: for example Malachi 4:1-6 is Malachi 3:19-24, Deuteronomy 12:32 is
   13:1, and Exodus 22:1 is 21:37. Each verse is moved to the numbering of the Hebrew text in `Data\MAM`.
   Where one English verse is two Hebrew verses (Numbers 26:1, 1 Samuel 20:42, 1 Kings 22:43) it is divided;
   where several are one (the four short commandments in Exodus 20:13 and Deuteronomy 5:17, which MAM keeps as
   one verse, and Isaiah 63:19 with 64:1) they are joined. The generator checks that every Hebrew verse of every
   book gets exactly one English verse.
2. **Names of G-d.** "the LORD", "LORD" and "GOD" (the divine name) are written "HaShem", and "God" is written
   "G-d". Everything else, including "Lord" (Adonai) and the English forms of other names (Moses, Abraham),
   is unchanged.

To update: download a newer `engwmb_vpl.zip`, then run

    cd Tools\EnglishGenerator
    dotnet run -- <path>\engwmb_vpl.txt

and update the download date and SHA-256 above.

These files replace the HNV-based English that came with TropeTrainer 4.1, which had no license statement and
had verses misnumbered in several chapters (e.g. 1 Kings 5, Isaiah 9, Jeremiah 31, Zechariah 2).
