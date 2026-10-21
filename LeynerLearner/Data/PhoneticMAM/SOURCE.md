# Phonetic MAM in Leyner Learner

This folder is an **unmodified copy** of the `Phonetic-MAM` folder of MAM-basics:

- Source: <https://github.com/bdenckla/MAM-basics/tree/202e69e8e063438b5faca0d73d20a5cde3d4fb61/Phonetic-MAM>
  (commit `202e69e8e063438b5faca0d73d20a5cde3d4fb61`, the same commit as the MAM text in `Data\MAM`), downloaded
  2026-10-20
- Files: `README.md` and `LICENSE.md` (the project's own), `schema\phonetic-mam-public-v1.schema.json`, and
  `data\*.json`: all 39 books of the Tanakh (Torah, Prophets and Writings), one file per book
- License: **CC BY-SA 4.0** (see `LICENSE.md`). Attribute the MAM Hebrew to Hebrew Wikisource,
  [Miqra according to the Masorah](https://en.wikisource.org/wiki/User:Dovi/Miqra_according_to_the_Masorah#beginning);
  Phonetic MAM's source attribution also names [Al-Hatorah Mikraot Gedolot](https://mg.alhatorah.org).
  The `examples` folder of the original, which has other terms, is not included.

Each book file gives every word of the MAM text with its **Sephardic** and **Ashkenazic** transcription: syllables
separated by `·`, the stressed syllable marked, and a sounded sheva (sheva na) written as a superscript e.
`README.md` describes the format and how its Hebrew differs from MAM's text (it writes words as they are read,
for example אֲדֹנָי for the divine name).

## How the app uses it

- **Transliteration** (the toolbar option): `Core\PhoneticMam.cs` reads the book files and matches their words
  to the app's MAM text in order, not by verse number, since Phonetic MAM divides a few chapters differently
  (Exodus 20, Numbers 25, Deuteronomy 5, Joshua 21, 1 Samuel 23-24, Jeremiah 30-31). Words written one way and
  read another (the divine name, הִוא read as הִיא, Jerusalem, Issachar) are paired by position. In the 26 books
  the app shows, every word gets its transliteration except three: Joshua 21:36-37, which Phonetic MAM does not
  have, and one word of Deuteronomy 32:6, which it joins with the next.
- **Sounded shevas** (`Data\MAM\sheva-na.json`): `Tools\ShevaNaGenerator` derives which shevas are sounded from
  the Sephardic transcriptions here. It reads this folder by default.

The Writings are included so the data is complete; the app does not show readings from them yet.

## Updating

Replace this folder's files with those of a newer MAM-basics commit (keeping this file), update the commit and
date above, then rebuild `Data\MAM\sheva-na.json`:

    cd Tools\ShevaNaGenerator
    dotnet run

Update `Data\MAM` from the same commit, so the two texts match.
