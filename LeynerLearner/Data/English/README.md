# English translation: JPS 1917

The English text in this folder is the **JPS 1917** translation, *The Holy Scriptures According to the Masoretic
Text: A New Translation*, published by the Jewish Publication Society of America in 1917.

- Source: eBible.org's edition, "JPS TaNaKH 1917" (<https://eBible.org/find/details.php?id=engjps>), file
  `engjps_vpl.zip` (verse-per-line text; eBible.org's text dated 2023-09-19), downloaded 2026-10-27,
  SHA-256 `2C23E2689258E3F457F887AAD1C1162CBCB7135450F14E49613C773CA205F7B8`
- License: **public domain** (published in 1917). eBible.org lists it as public domain; the Open Siddur Project,
  which JPS supports in sharing the 1917 text, publishes it under CC0:
  <https://opensiddur.org/readings-and-sourcetexts/mekorot/tanakh/translations/tanakh-the-holy-scriptures-a-new-translation-jps-1917/>

eBible.org's verse-per-line file was chosen over Open Siddur's plain text (a conversion of the printed page, with
footnote letters attached to words) because it has one verse per line with the notes removed.

## How the files were made

`Tools\EnglishGenerator` reads `engjps_vpl.txt` and writes one file per book (`Gen.txt`, `Exod.txt`, ...), one
verse per line as `chapter:verse text`, UTF-8. It makes these changes:

1. **Jewish verse numbering.** JPS 1917 numbers verses as the Hebrew does. eBible.org numbers them as English
   (Christian) Bibles do and writes JPS's number in parentheses where it differs, at the start of a verse
   (`GEN 31:55 (32-1) And early in the morning ...` is Genesis 32:1) or inside it where a Hebrew verse begins
   (`NUM 26:1 (25-19) And it came to pass after the plague, (26-1) that the LORD spoke ...`). The generator uses
   those numbers. Several English verses that are one Hebrew verse are joined (the four short commandments in
   Exodus 20:13 and Deuteronomy 5:17, and Isaiah 63:19). It checks that every verse of the Hebrew text in
   `Data\MAM` gets exactly one English verse.
2. **Book openings.** The first words of each book are printed in capitals ("AND MOAB rebelled"); they get their
   ordinary case ("And Moab rebelled").
3. **Names of G-d.** "the LORD", "LORD" and "GOD" (the divine name) are written "HaShem", and "God" is written
   "G-d". Everything else is unchanged.

To update: download a newer `engjps_vpl.zip`, then run

    cd Tools\EnglishGenerator
    dotnet run -- <path>\engjps_vpl.txt

and update the date and SHA-256 above (and run Tools\DataManifest).

These files replace the English based on the World Messianic Bible (2026-10-20 to 2026-10-27).
