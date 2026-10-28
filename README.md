# Leyner Learner

A Windows app for learning to read Torah (leyning) on the triennial cycle. For any Shabbat, holiday, fast day or
Rosh Chodesh it shows the Torah reading and haftarah, aliyah by aliyah, with vowels and trope, and can save it as a
PDF or print it.

- Triennial or full-kriyah aliyot, and Ashkenazi, Sephardi, Chabad or triennial haftarot
- Sounded shevas (sheva na) marked in color, with a large sheva, or with a rafe
- Sephardic or Ashkenazic transliteration, and English, under each aliyah
- Upper or lower trope for the Ten Commandments
- Help (F1) explains every source and the sheva na / sheva nach rules

Built with .NET 10 and Windows Forms; PDFs with [QuestPDF](https://www.questpdf.com) (Community license).

## Projects

| Folder | What it is |
|---|---|
| `LeynerLearner` | The app |
| `LeynerLearnerX` | An experimental copy of the app (with Verse-per-line and Tikkun layouts) |
| `Tools\EnglishGenerator` | Makes `Data\English` from the JPS 1917 translation (eBible.org), in the Hebrew verse numbering |
| `Tools\ShevaNaGenerator` | Makes `Data\MAM\sheva-na.json` (which shevas are sounded) from Phonetic MAM |
| `Tools\ShevaNaFont` | Builds the Hebrew Sheva Na font, with its corresponding source |
| `Tools\DataManifest` | Checks and updates `Data\manifest.json`, and makes data packs for "Update data" |
| `Tools\AppIcon` | Makes the app icon (a lamed) |

## Data and licenses

The app's code is under the MIT license (`LICENSE`). Its data keeps the licenses of its sources; each folder of
`LeynerLearner\Data` has a README.md or SOURCE.md with the details.

| Data | Source | License |
|---|---|---|
| Hebrew text (`MAM`) | Miqra according to the Masorah, via MAM-basics | CC BY-SA 4.0 |
| Transliteration and sounded shevas (`PhoneticMAM`, `MAM\sheva-na.json`) | Phonetic MAM | CC BY-SA 4.0 |
| Reading schedules (`Hebcal`) | @hebcal/triennial, @hebcal/leyning | BSD-2-Clause |
| English (`English`) | JPS 1917, The Holy Scriptures According to the Masoretic Text (eBible.org) | Public domain |
| Font (`Fonts`) | Hebrew Sheva Na, from Taamey D 0.921 | GPL-2.0 with font exception |
| Font (`FrankRuhlLibre`, optional) | Frank Ruhl Libre Regular, unmodified (no trope marks) | OFL-1.1 |
