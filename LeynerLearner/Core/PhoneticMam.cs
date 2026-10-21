using System.Text;
using System.Text.Json;

namespace LeynerLearner.Core;

/// <summary>Which transliteration to show.</summary>
public enum TransliterationStyle { Off, Sephardic, Ashkenazic }

/// <summary>A piece of a transliterated word: plain text, a sounded sheva (written as a superscript e), or
/// text in the stressed syllable.</summary>
public record PhoneticRun(string Text, bool SoundedSheva = false, bool Stressed = false);

/// <summary>One word's transliterations, in the Sephardic and Ashkenazic pronunciations (empty for a word whose
/// transliteration is given with the word before it).</summary>
public record PhoneticWord(IReadOnlyList<PhoneticRun> Sephardic, IReadOnlyList<PhoneticRun> Ashkenazic)
{
    public IReadOnlyList<PhoneticRun> For(TransliterationStyle style) =>
        style == TransliterationStyle.Ashkenazic ? Ashkenazic : Sephardic;
}

/// <summary>
/// Phonetic MAM (Data\PhoneticMAM, unmodified; see SOURCE.md there): the MAM text transliterated word by word
/// in Sephardic and Ashkenazic pronunciation.
///
/// Its words are matched to this app's verses in order, not by verse number: in a few chapters (Exodus 20,
/// Deuteronomy 5 and others) its verse division differs from the MAM files the app shows, and its Hebrew has
/// the spoken form where the app shows the written one (for example אֲדֹנָי for the divine name), as its
/// README explains.
/// </summary>
public class PhoneticMam(string dir, ScriptureText text)
{
    /// <summary>Phonetic MAM's file for each book the app shows (Data\PhoneticMAM\data\*.json).</summary>
    public static readonly IReadOnlyDictionary<string, string> Files = new Dictionary<string, string>
    {
        ["Gen"] = "A1-Genesis", ["Exod"] = "A2-Exodus", ["Lev"] = "A3-Levit", ["Num"] = "A4-Numbers", ["Deut"] = "A5-Deuter",
        ["Josh"] = "B1-Joshua", ["Judg"] = "B2-Judges", ["1Sam"] = "BA-1Samuel", ["2Sam"] = "BB-2Samuel",
        ["1Kgs"] = "BC-1Kings", ["2Kgs"] = "BD-2Kings", ["Isa"] = "C1-Isaiah", ["Jer"] = "C2-Jeremiah", ["Ezek"] = "C3-Ezekiel",
        ["Hos"] = "CA-Hosea", ["Joel"] = "CB-Joel", ["Amos"] = "CC-Amos", ["Obad"] = "CD-Obadiah", ["Jonah"] = "CE-Jonah",
        ["Mic"] = "CF-Micah", ["Nah"] = "CG-Nahum", ["Hab"] = "CH-Habakkuk", ["Zeph"] = "CI-Tsefaniah", ["Hag"] = "CJ-Haggai",
        ["Zech"] = "CK-Zechariah", ["Mal"] = "CL-Malachi",
    };

    /// <summary>One way a word may be written (its pointed form), with how it is read in each pronunciation.</summary>
    record Alternative(string Key, List<PhoneticRun>? Sephardic, List<PhoneticRun>? Ashkenazic);

    /// <summary>Optional: told where the two texts' words differ (for checking the matching).</summary>
    public Action<string>? Log { get; set; }

    // (book, upper cantillation) -> verse -> its words' transliterations (null where no word could be matched)
    readonly Dictionary<(string, bool), Dictionary<(int, int), PhoneticWord?[]>> cache = [];

    /// <summary>The transliteration of each word of a verse (as split by <see cref="ShevaNa.IsWord"/>), or null if
    /// the book has none.</summary>
    public PhoneticWord?[]? Words(string book, int chapter, int verse, bool upper)
    {
        if (!Files.ContainsKey(book)) return null;
        if (!cache.TryGetValue((book, upper), out var verses)) cache[(book, upper)] = verses = Align(book, upper);
        return verses.GetValueOrDefault((chapter, verse));
    }

    Dictionary<(int, int), PhoneticWord?[]> Align(string book, bool upper)
    {
        var rows = Load(book);

        // The app's words, in book order.
        var mine = new List<((int, int) Verse, int Word, string Key)>();
        var result = new Dictionary<(int, int), PhoneticWord?[]>();
        foreach (var v in text.Range(book, new(1, 1), new(999, 999)))
        {
            string hebrew = upper && v.HebrewUpper != null ? v.HebrewUpper : v.Hebrew;
            var words = hebrew.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(ShevaNa.IsWord).ToList();
            result[(v.Chapter, v.Number)] = new PhoneticWord?[words.Count];
            for (int w = 0; w < words.Count; w++) mine.Add(((v.Chapter, v.Number), w, Key(words[w])));
        }

        void Assign(int i, Alternative a) =>
            result[mine[i].Verse][mine[i].Word] = new PhoneticWord(a.Sephardic ?? a.Ashkenazic ?? [], a.Ashkenazic ?? a.Sephardic ?? []);
        Alternative? Match(int i, int j) =>
            i < mine.Count && j < rows.Count ? rows[j].FirstOrDefault(a => a.Key == mine[i].Key) : null;
        // Two words in a row match: enough to trust a skip (a single word such as אֱלֹהִים recurs too often).
        bool Resumes(int i, int j) => Match(i, j) != null && (i + 1 == mine.Count || Match(i + 1, j + 1) != null);

        // In order: equal words match; a few words on either side may have no partner (a verse one text lacks, a
        // paseq); otherwise the words are paired anyway: a word read differently from its spelling, such as the
        // divine name, which Phonetic MAM writes as it is read.
        int i = 0, j = 0;
        while (i < mine.Count && j < rows.Count)
        {
            if (Match(i, j) is { } a) { Assign(i++, a); j++; continue; }
            // Two of the app's words written as one (in the Ten Commandments): the first gets the transliteration.
            if (i + 1 < mine.Count && rows[j].FirstOrDefault(x => x.Key == mine[i].Key + mine[i + 1].Key) is { } joined)
            {
                Assign(i, joined);
                result[mine[i + 1].Verse][mine[i + 1].Word] = new PhoneticWord([], []);
                i += 2; j++;
                continue;
            }
            if (Resumes(i + 1, j + 1))
            {
                Log?.Invoke($"{book} {mine[i].Verse.Item1}:{mine[i].Verse.Item2} paired {mine[i].Key} / {rows[j][0].Key}");
                Assign(i++, rows[j++][0]);
                continue;
            }
            int skipRows = Enumerable.Range(1, 12).FirstOrDefault(d => Resumes(i, j + d));
            int skipMine = Enumerable.Range(1, 12).FirstOrDefault(d => Resumes(i + d, j));
            if (skipRows > 0 && (skipMine == 0 || skipRows <= skipMine))
            {
                Log?.Invoke($"{book} {mine[i].Verse.Item1}:{mine[i].Verse.Item2} skipped {skipRows} Phonetic MAM words");
                j += skipRows;
            }
            else if (skipMine > 0)
            {
                Log?.Invoke($"{book} {mine[i].Verse.Item1}:{mine[i].Verse.Item2} {skipMine} words without transliteration");
                i += skipMine;
            }
            else
            {
                Log?.Invoke($"{book} {mine[i].Verse.Item1}:{mine[i].Verse.Item2} paired {mine[i].Key} / {rows[j][0].Key}");
                Assign(i++, rows[j++][0]);
            }
        }
        return result;
    }

    /// <summary>The book's words in order, each with its alternatives (most words have one).</summary>
    List<List<Alternative>> Load(string book)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "data", Files[book] + ".json")));
        var words = new List<List<Alternative>>();
        foreach (var chapter in doc.RootElement.GetProperty("chapters").EnumerateArray())
            foreach (var verse in chapter.GetProperty("verses").EnumerateArray())
                foreach (var row in verse.GetProperty("rows").EnumerateArray())
                {
                    var hebrew = row.GetProperty("hebrew").EnumerateArray().ToList();
                    var seph = row.GetProperty("transcriptions").GetProperty("sephardic").EnumerateArray().ToList();
                    var ashk = row.GetProperty("transcriptions").GetProperty("ashkenazic").EnumerateArray().ToList();
                    var alternatives = new List<Alternative>();
                    for (int col = 0; col < hebrew.Count; col++)
                    {
                        if (hebrew[col].ValueKind != JsonValueKind.Array) continue;   // a paragraph break or empty cell
                        foreach (var (label, content) in Readings(hebrew[col]))
                        {
                            string key = Key(HebrewText(content));
                            if (key.Length == 0) continue;                               // paseq
                            alternatives.Add(new(key, Transcription(seph, col, label), Transcription(ashk, col, label)));
                        }
                    }
                    if (alternatives.Count > 0) words.Add(alternatives);
                }
        return words;
    }

    /// <summary>A cell's readings: one unlabelled reading, or labelled alternatives (two qamats readings, two cantillations).</summary>
    static IEnumerable<(string? Label, JsonElement Content)> Readings(JsonElement cell)
    {
        bool labelled = false;
        foreach (var item in cell.EnumerateArray())
            if (item.ValueKind == JsonValueKind.Object && item.GetProperty("kind").GetString() == "reading")
            {
                labelled = true;
                yield return (item.GetProperty("label").GetString(), item.GetProperty("content"));
            }
        if (!labelled) yield return (null, cell);
    }

    static string HebrewText(JsonElement content)
    {
        var sb = new StringBuilder();
        foreach (var item in content.EnumerateArray())
            if (item.ValueKind == JsonValueKind.String) sb.Append(item.GetString());
            else if (item.GetProperty("kind").GetString() == "implicit-maqaf") sb.Append('־');
        return sb.ToString();
    }

    /// <summary>The transcription in the same column with the same reading label, else the column's (or the only) one.</summary>
    static List<PhoneticRun>? Transcription(List<JsonElement> columns, int col, string? label)
    {
        foreach (int c in new[] { col, 0 }.Distinct().Where(c => c < columns.Count))
        {
            if (columns[c].ValueKind != JsonValueKind.Array) continue;
            foreach (var (l, content) in Readings(columns[c]))
                if (l == label || label == null || l == null)
                {
                    var runs = new List<PhoneticRun>();
                    Runs(content, false, runs);
                    if (runs.Count > 0 && runs.Any(r => r.Text.Any(char.IsLetter))) return runs;   // "—" means none
                }
        }
        return null;
    }

    static void Runs(JsonElement content, bool stressed, List<PhoneticRun> runs)
    {
        foreach (var item in content.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String) { runs.Add(new(item.GetString()!, Stressed: stressed)); continue; }
            switch (item.GetProperty("kind").GetString())
            {
                case "superscript-e": runs.Add(new("e", SoundedSheva: true, Stressed: stressed)); break;
                case "stressed": Runs(item.GetProperty("content"), true, runs); break;
                case "reading": Runs(item.GetProperty("content"), stressed, runs); break;
            }
        }
    }

    /// <summary>Pointed form used to match words: letters, vowels, dagesh, shin/sin dots, meteg and maqaf; no accents.</summary>
    static string Key(string w) =>
        new(w.Where(c => c is (>= 'א' and <= 'ת') or (>= 'ְ' and <= 'ֽ') or 'ׁ' or 'ׂ' or 'ׇ' or '־').ToArray());
}
