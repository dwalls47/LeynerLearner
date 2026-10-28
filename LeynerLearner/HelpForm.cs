using System.Diagnostics;
using System.Text;
using LeynerLearner.Core;
using LeynerLearner.Rendering;

namespace LeynerLearner
{
    /// <summary>The Help window: where the app's data comes from, and sheva na vs. sheva nach.</summary>
    public class HelpForm : Form
    {
        public HelpForm()
        {
            Text = "Leyner Learner Help";
            Icon = AppIcon.Value;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(860, 780);
            var rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = SystemColors.Window,
            };
            rtb.LinkClicked += (_, e) => Process.Start(new ProcessStartInfo(e.LinkText!) { UseShellExecute = true });
            Controls.Add(rtb);
            rtb.Rtf = Build();
        }

        // In the help text, "^" right after a sheva marks it as sounded: it becomes the large sheva.
        static string Build()
        {
            var h = new Rtf();
            h.Title("Leyner Learner Help");

            h.Heading("Data and resources");
            h.Text("Everything the app shows comes from its data folder. Nothing is downloaded unless you ask for an update. " +
                   "All of it is freely licensed or public domain, and each folder has a file saying where it came from.");

            h.Subheading("Where the data lives, and updating it");
            h.Text("The app comes with its data in the **Data** folder next to the program. Data\\manifest.json lists each " +
                   "source (where it came from, its version and license) and the size and SHA-256 fingerprint of every file.");
            h.Text("**Update data…** downloads the current data pack, a zip of the whole Data folder, and checks every file " +
                   "against the pack's manifest before using it. It then shows what changed (for example the MAM version, old → " +
                   "new) and asks before installing. **Install from file…** does the same with a pack you already have. " +
                   "Updated data is kept in " + DataStore.InstalledDir + "; the app's own Data folder is never changed.");
            h.Bullet("The updated data is used only while every file still matches the manifest. If one does not, or the " +
                     "app itself comes with newer data, the app uses its built-in data.");
            h.Bullet("**Use built-in data** deletes the updated copy and goes back to the data that came with the app.");
            h.Bullet("A pack always has all the data together, because some files are made from others: the sounded-sheva " +
                     "list (Data\\MAM\\sheva-na.json) comes from Phonetic MAM, and the English is numbered to match the MAM " +
                     "text, so they must come from the same version.");
            h.Text("Anyone can check or rebuild the data: Tools\\DataManifest recomputes the manifest's fingerprints and makes a " +
                   "pack (dotnet run -- pack LeynerLearner-data.zip).");

            h.Subheading("Hebrew text: Miqra according to the Masorah (MAM)");
            h.Text("The Torah and haftarah text, with vowels and trope, is **Miqra according to the Masorah**, prepared by " +
                   "Seth (Avi) Kadish at Hebrew Wikisource, from Ben Denckla's MAM-basics collection. It is licensed CC BY-SA 4.0, " +
                   "which is why every screen, PDF and printout carries an attribution line. Where a verse has two sets of trope " +
                   "(the Ten Commandments and Genesis 35:22), the **Upper trope** option chooses the upper cantillation (ta'am elyon). " +
                   "Folder: Data\\MAM.");
            h.Bullet("**Home page** (Hebrew Wikisource, English description): https://en.wikisource.org/wiki/User:Dovi/Miqra_according_to_the_Masorah");
            h.Bullet("**Home page** (Hebrew Wikisource, in Hebrew): " +
                     "https://he.wikisource.org/wiki/%D7%9E%D7%A7%D7%A8%D7%90_%D7%A2%D7%9C_%D7%A4%D7%99_%D7%94%D7%9E%D7%A1%D7%95%D7%A8%D7%94");
            h.Bullet("**Download** (MAM-basics, the data files): https://github.com/bdenckla/MAM-basics");
            h.Bullet("**The exact version in this app** (MAM-simple): " +
                     "https://github.com/bdenckla/MAM-basics/tree/202e69e8e063438b5faca0d73d20a5cde3d4fb61/MAM-simple");

            h.Subheading("Transliteration and sounded shevas: Phonetic MAM");
            h.Text("**Phonetic MAM** is the same MAM text transcribed word by word into Latin letters, in both the **Sephardic** " +
                   "and the **Ashkenazic** pronunciation. Like MAM it is CC BY-SA 4.0; its attribution also names Al-HaTorah " +
                   "Mikraot Gedolot. The app includes all of it, unchanged: all 39 books of the Tanakh, from the same version " +
                   "of MAM-basics as the Hebrew text. Folder: Data\\PhoneticMAM (SOURCE.md there tells where it came from).");
            h.Text("The **Transliteration** option shows it under each aliyah's Hebrew, on screen, in PDFs and in printouts:");
            h.Bullet("Syllables are separated by a raised dot: bə·re·shit is written bᵉ·ré·shít.");
            h.Bullet("A small raised **e** is a sounded sheva (sheva na). A silent sheva is simply not written.");
            h.Bullet("The stressed syllable is underlined.");
            h.Bullet("’ is alef, ‘ is ayin, ḥ is chet, and kh is a khaf without a dagesh. ă, ĕ and ŏ are the chataf vowels.");
            h.Bullet("Ashkenazic and Sephardic differ mainly in the kamatz (o or a: bo·ro or ba·ra) and in a tav without a " +
                     "dagesh (s or t: ’és or ’ét).");
            h.Text("Phonetic MAM writes each word as it is read, so the divine name appears as Adonai, and a word written one way " +
                   "and read another (such as הִוא, read hi) appears as it is read. Its words are matched to the Hebrew text in " +
                   "order, so they line up even in the few chapters where its verse numbers differ. Three words have no " +
                   "transliteration, shown as \"…\": Joshua 21:36-37, which Phonetic MAM does not include, and one word of " +
                   "Deuteronomy 32:6, which it joins to the next. The Writings are included for completeness; the app does not " +
                   "show readings from them yet.");
            h.Text("Which shevas are sounded (the **Sounded sheva** option) comes from the same transcriptions. A tool in this " +
                   "project (Tools\\ShevaNaGenerator) lines them up with the Hebrew text, sheva by sheva, and saves the result as " +
                   "Data\\MAM\\sheva-na.json. It covers the Torah and Prophets: about 55,000 sounded and 45,800 silent shevas. " +
                   "Shevas in the divine name are left unmarked, because it is read Adonai. The judgments are Phonetic MAM's; " +
                   "see \"Where traditions differ\" below.");

            h.Subheading("The Hebrew Sheva Na font (Large sheva)");
            h.Text("Unicode has no separate character for a sounded sheva, and no standard font draws one differently. So the app " +
                   "bundles its own font, **Hebrew Sheva Na**, made for this app from an existing free font with one glyph added: " +
                   "a sheva drawn 1.4 times its normal size, attached under the letter exactly like an ordinary sheva. It also " +
                   "draws the app's icon, a lamed. Folder: Data\\Fonts.");
            h.Text("Where it comes from:");
            h.Bullet("**Taamey D 0.921**, by Ben Denckla, is the font Hebrew Sheva Na is built from. It is the font that MAM's own " +
                     "pages use, and its rules for placing vowels and trope marks follow MAM's text. It is published as part of " +
                     "MAM-basics, the same project as the Hebrew text.");
            h.Bullet("**Taamey Frank CLM**, by Yoram Gnat, is where Taamey D's letters, vowels and trope marks come from. It " +
                     "belongs to the **Culmus** project, a set of free Hebrew fonts.");
            h.Bullet("**Taamey D home page and download** (its published font, doc\\woff2\\Taamey_D.woff2, and its source " +
                     "package, in\\font-support\\taamey-d-0.921, in the version this app uses): " +
                     "https://github.com/bdenckla/MAM-basics/tree/202e69e8e063438b5faca0d73d20a5cde3d4fb61/in/font-support/taamey-d-0.921");
            h.Bullet("**Culmus home page** (Taamey Frank CLM and the other Culmus fonts): https://culmus.sourceforge.io/ ; " +
                     "its fonts with trope marks: https://culmus.sourceforge.io/taamim/");
            h.Bullet("**Culmus download:** https://sourceforge.net/projects/culmus/files/");
            h.Text("The app marks each sounded sheva by putting an invisible character, COMBINING GRAPHEME JOINER (U+034F), right " +
                   "after it. The font draws that sheva-plus-joiner pair as the large sheva. Other fonts ignore the joiner and show " +
                   "an ordinary sheva, and text copied from the app or its PDFs is still ordinary Hebrew. That is why choosing " +
                   "**Large sheva** also selects this font. The **Color** option works with any font instead: it draws the letter " +
                   "carrying a sounded sheva in red. **Large sheva and color** does both, for the clearest marking.");
            h.Text("**Sheva with rafe** marks a sounded sheva differently: it adds a rafe, the short bar above the letter, as in " +
                   ShevaNa.Mark("שְׁמַע", [(0, 0)], ShevaNaStyle.ShevaRafe) + ". It works with almost any font (nearly all Hebrew fonts have the rafe), and **Sheva with rafe and " +
                   "color** also draws the letter in red. Two things to know:");
            h.Bullet("The Hebrew text itself has a real rafe on 63 letters, showing that a letter is soft or not pronounced. " +
                     "Only one of them also has a sheva (Ezekiel 32:30), so elsewhere a rafe together with a sheva is the app's " +
                     "marking.");
            h.Bullet("Traditionally a rafe means a letter has no dagesh, so on a letter with a dagesh (as in בְּרֵאשִׁית) the " +
                     "two marks look contradictory. Here the rafe only marks the sheva as sounded. Text copied with this " +
                     "option keeps the added rafe marks.");
            h.Text("The font is loaded only while the app is running; it is not installed in Windows. Its license is the GNU GPL " +
                   "version 2 with a font-embedding exception, so PDFs that embed it are not themselves covered by the GPL. " +
                   "Taamey D's license reserves its name, so a modified copy must have a new one; hence Hebrew Sheva Na. Its " +
                   "corresponding source (Taamey D's source package) and the build script are in Tools\\ShevaNaFont.");

            h.Subheading("The Frank Ruhl Libre font");
            h.Text("**Frank Ruhl Libre** is Yanek Iontef's open-source revival of Frank Rühl, the classic Hebrew typeface that " +
                   "Rafael Frank designed in 1908-1910 and the most common Hebrew typeface in print. The app includes its Regular " +
                   "weight, unchanged, and lists it in the **Font** list. It is under the **SIL Open Font License 1.1**, which " +
                   "allows it to be bundled and shared with software. Folder: Data\\FrankRuhlLibre (SOURCE.md there tells where " +
                   "it came from).");
            h.Text("It has every letter and vowel point but **no trope marks**. With it, the trope is drawn from another font " +
                   "and some marks sit slightly out of place, so it is best for reading with vowels only. For the Frank Rühl " +
                   "style with trope, use Hebrew Sheva Na: its letters come from Taamey Frank CLM, also a Frank Rühl design.");
            h.Bullet("**Home page and download** (all weights and formats): https://github.com/fontef/frankruhllibre");
            h.Bullet("**Google Fonts page:** https://fonts.google.com/specimen/Frank+Ruhl+Libre");
            h.Bullet("**The designer, Fontef:** http://fontef.com");
            h.Text("**Adding the missing trope marks yourself.** Because Frank Ruhl Libre is under the Open Font License, anyone " +
                   "may make a derivative font with the cantillation marks added, using a free font editor such as " +
                   "**FontForge**. In outline:");
            h.Bullet("Copy the trope marks (U+0591-U+05AF) from another font under the Open Font License, such as Noto Serif " +
                     "Hebrew or Ezra SIL, and scale them to match Frank Ruhl Libre's vowels. Do not copy them from a GPL font " +
                     "such as Taamey Frank CLM or Hebrew Sheva Na: the two licenses cannot be combined in one font.");
            h.Bullet("Give each mark an anchor, so it attaches above or below the letter like the vowels do, and check that " +
                     "trope and vowels under the same letter sit side by side rather than overlapping. This takes the most " +
                     "care: look at real verses at a large size and adjust.");
            h.Bullet("Optionally add the sounded sheva the way Hebrew Sheva Na does: a larger copy of the sheva that replaces " +
                     "a sheva followed by the invisible joiner U+034F (Tools\\ShevaNaFont\\build_font.py in this project shows how).");
            h.Bullet("Keep the Open Font License and the copyright notices of both fonts, and give the result a new name.");
            h.Bullet("**FontForge** (free, for Windows, Mac and Linux): https://fontforge.org/ ; download: " +
                     "https://fontforge.org/en-US/downloads/");
            h.Bullet("**Noto Serif Hebrew** (all trope marks, Open Font License): https://fonts.google.com/noto/specimen/Noto+Serif+Hebrew");
            h.Bullet("**Ezra SIL** (all trope marks, Open Font License): https://software.sil.org/ezra/");
            h.Bullet("**The Open Font License and its FAQ** (what a derivative font must do): https://openfontlicense.org/ofl-faq/");

            h.Subheading("Reading schedules: Hebcal");
            h.Text("The triennial aliyot, the alternative triennial haftarot, the full-kriyah haftarot (Ashkenazi, Sephardi and " +
                   "Chabad) and the readings for holidays, fast days, Rosh Chodesh and special Shabbatot come from Hebcal " +
                   "(https://www.hebcal.com): the @hebcal/triennial 6.4.0 and @hebcal/leyning 10.1.0 packages, under the " +
                   "BSD 2-clause license. Folder: Data\\Hebcal.");
            h.Text("These files are tables, not a list of dates: for each parashah, the aliyot for each year of the three-year " +
                   "cycle. The app works out the Hebrew calendar itself (which parashah falls on which Shabbat, when portions are " +
                   "combined, and which year of the cycle it is), so it already has every week from October 1983 through 2199, " +
                   "with nothing more to download. New versions of Hebcal's tables (corrections, or changes the Rabbinical " +
                   "Assembly adopts) arrive with **Update data**.");
            h.Text("Hebcal's triennial cycle is the one published by the Rabbinical Assembly's Committee on Jewish Law and " +
                   "Standards: Richard Eisenberg's \"A Complete Triennial System for Reading the Torah\", with later changes by " +
                   "Rabbi Joshua Heller (2012) and Rabbi Miles B. Cohen (2020).");
            h.Bullet("**Home page** (Hebcal; its Torah readings, with the triennial aliyot for each week): https://www.hebcal.com/sedrot/");
            h.Bullet("**Download** (the triennial tables): https://github.com/hebcal/hebcal-triennial");
            h.Bullet("**Download** (the full-kriyah and holiday tables): https://github.com/hebcal/hebcal-leyning");
            h.Bullet("**The triennial system** (Eisenberg, Rabbinical Assembly): " +
                     "https://www.rabbinicalassembly.org/sites/default/files/public/halakhah/teshuvot/19861990/eisenberg_triennial.pdf");
            h.Bullet("**Later changes:** https://www.rabbinicalassembly.org/sites/default/files/public/halakhah/teshuvot/2011-2020/heller-triennial-emendation.pdf " +
                     "and https://www.rabbinicalassembly.org/sites/default/files/2021-09/cohen-triennial.pdf");
            h.Bullet("**Sefaria** (each week's parashah with its text, translations and commentaries): " +
                     "https://www.sefaria.org/topics/category/torah-portions");

            h.Subheading("English translation");
            h.Text("The **English** option shows the **JPS 1917** translation, \"The Holy Scriptures According to the Masoretic " +
                   "Text\", published by the Jewish Publication Society of America in 1917. It was the standard Jewish English " +
                   "translation for most of the twentieth century, made from the Hebrew text by Jewish scholars for Jewish " +
                   "readers. Its English is in the style of its time (\"thou shalt\", \"unto\"). It is in the **public domain**: " +
                   "anyone may copy, change and share it. It covers every book the app shows. Folder: Data\\English.");
            h.Text("The app's copy comes from eBible.org's edition, which keeps each verse in a structured file. The Open " +
                   "Siddur Project, which JPS supports in sharing the 1917 text, has the full text and the scanned book.");
            h.Bullet("**Home page** (JPS 1917 at eBible.org): https://ebible.org/find/show.php?id=engjps");
            h.Bullet("**Download** (all formats; the app uses the verse-per-line text, engjps_vpl.zip): " +
                     "https://ebible.org/find/details.php?id=engjps");
            h.Bullet("**The Open Siddur Project's page** (the full text as one file, and the scans): " +
                     "https://opensiddur.org/readings-and-sourcetexts/mekorot/tanakh/translations/tanakh-the-holy-scriptures-a-new-translation-jps-1917/");
            h.Bullet("**About the translation:** https://en.wikipedia.org/wiki/Jewish_Publication_Society_of_America_Version");
            h.Bullet("**The Jewish Publication Society:** https://jps.org/");
            h.Text("The app changes the published text in two ways:");
            h.Bullet("**Jewish chapter and verse numbers.** JPS 1917 numbers verses as the Hebrew does, but eBible.org's " +
                     "edition renumbers them as English Bibles do and keeps the Hebrew numbers alongside. The app uses the " +
                     "Hebrew numbers, so each English verse appears beside the Hebrew verse it translates. For example, " +
                     "Malachi 4:1-6 in Christian Bibles is Malachi 3:19-24, and Isaiah 9:6 is 9:5 (\"For a child is born unto " +
                     "us\"). Where several English verses make one Hebrew verse, such as \"Thou shalt not murder. Thou shalt " +
                     "not commit adultery. Thou shalt not steal. Thou shalt not bear false witness\", they are joined.");
            h.Bullet("**Names of G-d.** The divine name, which the translation writes \"the LORD,\" is written \"HaShem,\" and " +
                     "\"God\" is written \"G-d,\" as many Jewish readers prefer.");
            h.Text("Tools\\EnglishGenerator makes the files from eBible.org's download, so a newer edition can be brought in the " +
                   "same way; Data\\English\\README.md has the details.");

            h.Subheading("JPS 1917 Modern");
            h.Text("Choosing **JPS 1917 Modern** in the **English** list shows the same JPS 1917 translation with its " +
                   "old-fashioned English put into modern English. It is made by this app, not published by the Jewish " +
                   "Publication Society, and is dedicated to the public domain (CC0) like the text it comes from. " +
                   "Folder: Data\\EnglishModern.");
            h.Text("A tool in this project (Tools\\EnglishModernizer) applies a fixed list of rules, so every change can be " +
                   "listed and checked, and the result is the same each time:");
            h.Bullet("**Pronouns:** thou, thee and ye become you; thy becomes your; thine and mine become your/yours and my/mine. " +
                     "Capitals for G-d are kept (Thou becomes You).");
            h.Bullet("**Verbs:** \"thou knowest\" becomes \"you know\", \"he cometh\" becomes \"he comes\", \"hath\" becomes " +
                     "\"has\". \"Shall\" becomes \"will\", except in commands addressed to you (\"You shall not murder\"). " +
                     "The \"thou\" of a command is dropped: \"be thou strong\" becomes \"be strong\".");
            h.Bullet("**Words and phrases:** unto becomes to, thereof becomes of it, wherefore becomes why or therefore, " +
                     "\"it came to pass\" becomes \"it happened\", \"I pray thee\" becomes \"please\", \"behold\" is dropped " +
                     "inside a sentence or becomes \"Look,\" at its start, and old words such as brethren, kine and raiment " +
                     "become brothers, cows and clothes. Spelling is American (neighbor, honor).");
            h.Bullet("**Sentence forms:** \"fear not\" becomes \"do not fear\", \"knew not\" becomes \"did not know\", \"G-d did " +
                     "prove Abraham\" becomes \"G-d proved Abraham\", \"Joab is fled\" becomes \"Joab has fled\", and \"Make thee an " +
                     "ark\" becomes \"Make yourself an ark\".");
            h.Text("About 170 verses that the rules can't handle well were edited by hand (for example \"did compass it round " +
                   "about\" becomes \"encircled it\"). Each hand edit is listed in Tools\\EnglishModernizer\\polish-edits.tsv. " +
                   "Sentence order is kept, so a few verses keep a formal ring. Many of the rules follow the choices of Adam " +
                   "Cohn's \"Modernized Tanakh\", which modernizes JPS 1917 for Genesis and Jonah.");
            h.Bullet("**Adam Cohn's Modernized Tanakh** (on Sefaria, CC-BY): https://www.sefaria.org/Genesis.1?ven=Modernized_Tanakh_-_Based_on_JPS_1917,_Edited_by_Adam_Cohn ; " +
                     "his site: http://modernizedtanakh.blogspot.com");

            h.Subheading("PDF and printing");
            h.Text("PDFs and printouts are made with QuestPDF (Community license), which shapes Hebrew vowels and trope with " +
                   "HarfBuzz, so they match what you see on screen.");

            h.Heading("Sheva na and sheva nach");
            h.Text("A sheva is two vertical dots under a letter, as in בְ. It is written the same way whether or not it is pronounced, " +
                   "so a reader has to know which kind it is.");
            h.Bullet("**Sheva na** (שְׁ^וָא נָע, \"moving sheva\") is sounded: a very short, unstressed vowel, like a quick \"uh\" " +
                     "or the first e in \"begin.\" It is written ə in the readings below. It does not end a syllable; it leans " +
                     "on the syllable after it.");
            h.Bullet("**Sheva nach** (שְׁ^וָא נָח, \"resting sheva\") is silent. It means the letter has no vowel, and it closes " +
                     "the syllable before it.");
            h.Text("Getting this right is part of reading correctly: sounding a silent sheva, or swallowing a sounded one, changes " +
                   "how the word is said. In this app's examples and readings, a **large sheva** is sounded and a normal-size " +
                   "sheva is silent.");

            h.Subheading("A sheva is usually sounded (na)");
            h.Bullet("At the beginning of a word:");
            h.Example("בְּ^רֵאשִׁית", "bə·re·shit");
            h.Example("שְׁ^מַע", "shə·ma");
            h.Bullet("The second of two shevas in a row in the middle of a word (the first one is silent):");
            h.Example("יִשְׁמְ^רוּ", "yish·mə·ru");
            h.Bullet("Under a letter doubled by a dagesh (dagesh chazak):");
            h.Example("הַמְּ^לָכִים", "ham·mə·la·khim");
            h.Bullet("Under the first of two identical letters:");
            h.Example("הַלְ^לוּ", "ha·lə·lu");
            h.Bullet("After a long vowel, usually marked with a meteg (the small vertical line beside the vowel):");
            h.Example("שָֽׁמְ^רוּ", "sha·mə·ru");

            h.Subheading("A sheva is usually silent (nach)");
            h.Bullet("After a short vowel in the middle of a word:");
            h.Example("מִדְבָּר", "mid·bar");
            h.Bullet("The first of two shevas in a row (see יִשְׁמְ^רוּ above).");
            h.Bullet("At the end of a word, including both shevas when a word ends with two:");
            h.Example("לֶךְ", "lekh");
            h.Example("וַיֵּשְׁתְּ", "vay·yesht");

            h.Subheading("Chataf vowels are always sounded");
            h.Text("The compound shevas, as in אֲ אֱ אֳ (chataf patach, chataf segol, chataf kamatz) are a sheva joined to a vowel. They are " +
                   "always sounded, as a short a, e or o, so the app does not mark them.");
            h.Example("אֲנִי", "a·ni");

            h.Subheading("Where traditions differ");
            h.Text("Grammarians and reading traditions do not agree on every sheva, especially after a long vowel without a meteg " +
                   "and after the conjunction וּ (\"and\"). The app follows Phonetic MAM throughout. For example, Phonetic MAM " +
                   "treats the sheva after an initial וּ as silent, so וּבְלֶכְתְּ^ךָ is read uv·lekh·tə·kha, where some grammars " +
                   "read u·və·lekh·tə·kha. If your teacher or tikkun reads a word differently, follow them.");

            return h.ToString();
        }

        /// <summary>Builds the help page's RTF. Runs of Hebrew are set in the bundled font automatically.</summary>
        sealed class Rtf
        {
            readonly StringBuilder sb = new();
            const string Para = @"\pard\li360\ri360\ltrpar\ql\f1\cf1";

            public Rtf()
            {
                sb.Append(@"{\rtf1\ansi\ansicpg1252\deff1");
                sb.Append(@"{\fonttbl{\f0\fnil\fcharset177 ").Append(ShevaNa.FontFamily).Append(@";}{\f1\fswiss\fcharset0 Segoe UI;}}");
                sb.Append(@"{\colortbl;\red0\green0\blue0;\red110\green110\blue110;\red40\green80\blue160;}");
            }

            public void Title(string s) => sb.Append(Para).Append(@"\sb120\sa120\fs36\b ").Append(RtfRenderer.Escape(s)).Append(@"\b0\par");

            public void Heading(string s) =>
                sb.Append(Para).Append(@"\sb360\sa120\fs30\b\cf3 ").Append(RtfRenderer.Escape(s)).Append(@"\b0\cf1\par");

            public void Subheading(string s) =>
                sb.Append(Para).Append(@"\sb200\sa60\fs22\b ").Append(RtfRenderer.Escape(s)).Append(@"\b0\par");

            public void Text(string s) => sb.Append(Para).Append(@"\sa120\fs20 ").Append(Inline(s, 26)).Append(@"\par");

            public void Bullet(string s) =>
                sb.Append(Para).Append(@"\fi-240\li600\tx600\sa80\fs20 ").Append(@"舦?\tab ").Append(Inline(s, 26)).Append(@"\par");

            /// <summary>A Hebrew word, large, with its reading.</summary>
            public void Example(string hebrew, string reading) =>
                sb.Append(Para).Append(@"\li960\tx2600\sa60\fs20 ").Append(Inline(hebrew, 40))
                  .Append(@"\tab\cf2 ").Append(RtfRenderer.Escape(reading)).Append(@"\cf1\par");

            /// <summary>Escapes text, with **bold** and Hebrew runs set in the Hebrew font at <paramref name="hebrewSize"/> half-points.</summary>
            static string Inline(string s, int hebrewSize)
            {
                s = MarkSounded(s);
                var sb = new StringBuilder();
                bool bold = false;
                for (int i = 0; i < s.Length;)
                {
                    if (s[i] == '*' && i + 1 < s.Length && s[i + 1] == '*')
                    {
                        bold = !bold;
                        sb.Append(bold ? @"\b " : @"\b0 ");
                        i += 2;
                        continue;
                    }
                    if (IsHebrew(s[i]))
                    {
                        int end = i;
                        // A Hebrew run continues across spaces between Hebrew words.
                        while (end < s.Length && (IsHebrew(s[end]) || s[end] == ' ' && end + 1 < s.Length && IsHebrew(s[end + 1]))) end++;
                        sb.Append(@"{\rtlch\f0\af0\fs").Append(hebrewSize).Append(@"\afs").Append(hebrewSize).Append(' ')
                          .Append(RtfRenderer.Escape(s[i..end])).Append('}');
                        i = end;
                        continue;
                    }
                    sb.Append(RtfRenderer.Escape(s[i].ToString()));
                    i++;
                }
                return sb.ToString();
            }

            /// <summary>Replaces each "^" with the joiner, placed right after the sheva before it (a dagesh or
            /// shin dot may come between them), as ShevaNa.Mark does for the readings.</summary>
            static string MarkSounded(string s)
            {
                var sb = new StringBuilder(s);
                for (int i = 0; i < sb.Length; i++)
                {
                    if (sb[i] != '^') continue;
                    sb.Remove(i, 1);
                    int sheva = i - 1;
                    while (sheva >= 0 && sb[sheva] != 'ְ' && !ShevaNa.IsLetter(sb[sheva])) sheva--;
                    if (sheva >= 0 && sb[sheva] == 'ְ') sb.Insert(sheva + 1, ShevaNa.Joiner);
                }
                return sb.ToString();
            }

            static bool IsHebrew(char c) => c is (>= '֑' and <= '״') or ShevaNa.Joiner;

            public override string ToString() => sb.ToString() + "}";
        }
    }
}
