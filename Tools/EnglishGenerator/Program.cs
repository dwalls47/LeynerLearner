// Generates Data\English\<book>.txt: the World Messianic Bible's text of each book the app shows, renumbered to
// the app's (MAM's) Hebrew verse numbering, with the divine name written "HaShem" and "God" written "G-d".
using System.Text;
using System.Text.RegularExpressions;
using LeynerLearner.Core;

// usage: EnglishGenerator <engwmb_vpl.txt> [app Data dir]
//   engwmb_vpl.txt is in https://eBible.org/Scriptures/engwmb_vpl.zip
if (args.Length < 1) { Console.Error.WriteLine("usage: EnglishGenerator <engwmb_vpl.txt> [app Data dir]"); return 1; }
string data = args.Length > 1 ? args[1] : Path.Combine("..", "..", "LeynerLearner", "Data");

var codes = new Dictionary<string, string>
{
    ["GEN"] = "Gen", ["EXO"] = "Exod", ["LEV"] = "Lev", ["NUM"] = "Num", ["DEU"] = "Deut", ["JOS"] = "Josh", ["JDG"] = "Judg",
    ["1SA"] = "1Sam", ["2SA"] = "2Sam", ["1KI"] = "1Kgs", ["2KI"] = "2Kgs", ["ISA"] = "Isa", ["JER"] = "Jer", ["EZE"] = "Ezek",
    ["HOS"] = "Hos", ["JOE"] = "Joel", ["AMO"] = "Amos", ["OBA"] = "Obad", ["JON"] = "Jonah", ["MIC"] = "Mic", ["NAH"] = "Nah",
    ["HAB"] = "Hab", ["ZEP"] = "Zeph", ["HAG"] = "Hag", ["ZEC"] = "Zech", ["MAL"] = "Mal",
};

// ---- Read the WMB (English verse numbering) ----
var english = new Dictionary<string, SortedDictionary<(int, int), string>>();
foreach (var line in File.ReadLines(args[0], Encoding.UTF8))
{
    var m = Regex.Match(line, @"^(\S+) (\d+):(\d+) (.*)$");
    if (!m.Success || !codes.TryGetValue(m.Groups[1].Value, out var osis)) continue;
    (english.TryGetValue(osis, out var b) ? b : english[osis] = [])
        [(int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value))] = m.Groups[4].Value.Trim();
}

// ---- Renumber to the Hebrew verse numbering ----
var text = new ScriptureText(data);
int problems = 0;
foreach (var book in Books.All)
{
    var hebrew = new SortedDictionary<(int, int), string>();
    void Put((int, int) at, string s)
    {
        if (!hebrew.TryAdd(at, s)) { Console.WriteLine($"{book.Osis} {at}: two English verses"); problems++; }
    }

    var merged = Versification.Merges.Where(x => x.Book == book.Osis).ToList();
    foreach (var merge in merged)
        Put(merge.Hebrew, string.Join(" ", merge.English.Select(e => english[book.Osis][e])));

    foreach (var ((c, v), s) in english[book.Osis])
    {
        if (merged.Any(x => x.English.Contains((c, v)))) continue;
        if (Versification.Splits.FirstOrDefault(x => x.Book == book.Osis && x.Chapter == c && x.Verse == v) is { } split)
        {
            int at = s.IndexOf(split.SecondStarts, StringComparison.Ordinal);
            if (at <= 0) { Console.WriteLine($"{book.Osis} {c}:{v}: split text not found"); problems++; Put(split.First, s); continue; }
            Put(split.First, s[..at].Trim());
            Put(split.Second, s[at..].Trim());
            continue;
        }
        Put(Versification.Map(book.Osis, c, v), s);
    }

    // Every verse of the Hebrew text should have exactly one English verse.
    var verses = text.Range(book.Osis, new(1, 1), new(999, 999)).Select(x => (x.Chapter, x.Number)).ToHashSet();
    foreach (var missing in verses.Where(x => !hebrew.ContainsKey(x)))
    { Console.WriteLine($"{book.Osis} {missing.Chapter}:{missing.Number}: no English"); problems++; }
    foreach (var extra in hebrew.Keys.Where(x => !verses.Contains(x)))
    { Console.WriteLine($"{book.Osis} {extra.Item1}:{extra.Item2}: no such Hebrew verse"); problems++; }

    Directory.CreateDirectory(Path.Combine(data, "English"));
    File.WriteAllLines(Path.Combine(data, "English", book.Osis + ".txt"),
        hebrew.Select(kv => $"{kv.Key.Item1}:{kv.Key.Item2} {NameStyle.Apply(kv.Value)}"), new UTF8Encoding(false));
}
Console.WriteLine(problems == 0 ? "OK: every Hebrew verse has exactly one English verse." : $"{problems} problems");
return problems == 0 ? 0 : 2;

/// <summary>The divine name and "God" as Jewish readers often write them.</summary>
static class NameStyle
{
    public static string Apply(string s)
    {
        s = Regex.Replace(s, @"\b[Tt]he LORD\b", "HaShem");      // the LORD, the LORD’s
        s = Regex.Replace(s, @"\b(LORD|GOD)\b", "HaShem");       // "O LORD", "Lord GOD"
        s = Regex.Replace(s, @"\bGod\b", "G-d");                 // God, God’s (not "gods")
        return s;
    }
}

/// <summary>
/// Where English Bibles number verses differently from the Hebrew. Each move gives an English range and where
/// it starts in the Hebrew; a split is an English verse that is two Hebrew verses, divided where the second begins.
/// </summary>
static class Versification
{
    public record Move(string Book, int Chapter, int From, int To, int HebrewChapter, int HebrewFrom);
    public record Split(string Book, int Chapter, int Verse, string SecondStarts, (int, int) First, (int, int) Second);

    public static readonly Move[] Moves =
    [
        new("Gen", 31, 55, 55, 32, 1), new("Gen", 32, 1, 32, 32, 2),
        new("Exod", 8, 1, 4, 7, 26), new("Exod", 8, 5, 32, 8, 1),
        new("Exod", 20, 17, 26, 20, 14),
        new("Exod", 22, 1, 1, 21, 37), new("Exod", 22, 2, 31, 22, 1),
        new("Lev", 6, 1, 7, 5, 20), new("Lev", 6, 8, 30, 6, 1),
        new("Num", 16, 36, 50, 17, 1), new("Num", 17, 1, 13, 17, 16),
        new("Num", 29, 40, 40, 30, 1), new("Num", 30, 1, 16, 30, 2),
        new("Deut", 5, 21, 33, 5, 18),
        new("Deut", 12, 32, 32, 13, 1), new("Deut", 13, 1, 18, 13, 2),
        new("Deut", 22, 30, 30, 23, 1), new("Deut", 23, 1, 25, 23, 2),
        new("Deut", 29, 1, 1, 28, 69), new("Deut", 29, 2, 29, 29, 1),
        new("1Sam", 21, 1, 15, 21, 2),
        new("1Sam", 23, 29, 29, 24, 1), new("1Sam", 24, 1, 22, 24, 2),
        new("2Sam", 18, 33, 33, 19, 1), new("2Sam", 19, 1, 43, 19, 2),
        new("1Kgs", 4, 21, 34, 5, 1), new("1Kgs", 5, 1, 18, 5, 15),
        new("1Kgs", 22, 44, 53, 22, 45),
        new("2Kgs", 11, 21, 21, 12, 1), new("2Kgs", 12, 1, 21, 12, 2),
        new("Isa", 9, 1, 1, 8, 23), new("Isa", 9, 2, 21, 9, 1),
        new("Isa", 64, 2, 12, 64, 1),
        new("Jer", 9, 1, 1, 8, 23), new("Jer", 9, 2, 26, 9, 1),
        new("Ezek", 20, 45, 49, 21, 1), new("Ezek", 21, 1, 32, 21, 6),
        new("Hos", 1, 10, 11, 2, 1), new("Hos", 2, 1, 23, 2, 3),
        new("Hos", 11, 12, 12, 12, 1), new("Hos", 12, 1, 14, 12, 2),
        new("Hos", 13, 16, 16, 14, 1), new("Hos", 14, 1, 9, 14, 2),
        new("Joel", 2, 28, 32, 3, 1), new("Joel", 3, 1, 21, 4, 1),
        new("Jonah", 1, 17, 17, 2, 1), new("Jonah", 2, 1, 10, 2, 2),
        new("Mic", 5, 1, 1, 4, 14), new("Mic", 5, 2, 15, 5, 1),
        new("Nah", 1, 15, 15, 2, 1), new("Nah", 2, 1, 13, 2, 2),
        new("Zech", 1, 18, 21, 2, 1), new("Zech", 2, 1, 13, 2, 5),
        new("Mal", 4, 1, 6, 3, 19),
    ];

    public static readonly Split[] Splits =
    [
        new("Num", 26, 1, "the LORD spoke", (25, 19), (26, 1)),
        new("1Sam", 20, 42, "He arose", (20, 42), (21, 1)),
        new("1Kgs", 22, 43, "However,", (22, 43), (22, 44)),
    ];

    /// <summary>English verses that are one Hebrew verse: in MAM's numbering, "do not murder / commit adultery /
    /// steal / bear false witness" is a single verse in both tellings of the Ten Commandments.</summary>
    public record Merge(string Book, (int, int)[] English, (int, int) Hebrew);

    public static readonly Merge[] Merges =
    [
        new("Exod", [(20, 13), (20, 14), (20, 15), (20, 16)], (20, 13)),
        new("Deut", [(5, 17), (5, 18), (5, 19), (5, 20)], (5, 17)),
        new("Isa", [(63, 19), (64, 1)], (63, 19)),
    ];

    public static (int, int) Map(string book, int c, int v) =>
        Moves.FirstOrDefault(m => m.Book == book && m.Chapter == c && v >= m.From && v <= m.To) is { } m
            ? (m.HebrewChapter, m.HebrewFrom + v - m.From)
            : (c, v);
}
