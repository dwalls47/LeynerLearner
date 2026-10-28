// Generates Data\English\<book>.txt: the JPS 1917 translation ("The Holy Scriptures According to the Masoretic Text")
// of each book the app shows, in the app's (MAM's) Hebrew verse numbering, with the divine name written "HaShem" and
// "God" written "G-d".
using System.Text;
using System.Text.RegularExpressions;
using LeynerLearner.Core;

// usage: EnglishGenerator <engjps_vpl.txt> [app Data dir]
//   engjps_vpl.txt is in https://eBible.org/Scriptures/engjps_vpl.zip (eBible.org's edition of JPS 1917, public domain)
if (args.Length < 1) { Console.Error.WriteLine("usage: EnglishGenerator <engjps_vpl.txt> [app Data dir]"); return 1; }
string data = args.Length > 1 ? args[1] : Path.Combine("..", "..", "LeynerLearner", "Data");

var codes = new Dictionary<string, string>
{
    ["GEN"] = "Gen", ["EXO"] = "Exod", ["LEV"] = "Lev", ["NUM"] = "Num", ["DEU"] = "Deut", ["JOS"] = "Josh", ["JDG"] = "Judg",
    ["1SA"] = "1Sam", ["2SA"] = "2Sam", ["1KI"] = "1Kgs", ["2KI"] = "2Kgs", ["ISA"] = "Isa", ["JER"] = "Jer", ["EZE"] = "Ezek",
    ["HOS"] = "Hos", ["JOE"] = "Joel", ["AMO"] = "Amos", ["OBA"] = "Obad", ["JON"] = "Jonah", ["MIC"] = "Mic", ["NAH"] = "Nah",
    ["HAB"] = "Hab", ["ZEP"] = "Zeph", ["HAG"] = "Hag", ["ZEC"] = "Zech", ["MAL"] = "Mal",
};

// ---- Read the text. eBible.org numbers verses as English Bibles do and gives JPS's own (Hebrew) number in
// parentheses where it differs: "GEN 31:55 (32-1) And early in the morning..." is Genesis 32:1. A marker inside a
// verse starts the next Hebrew verse: "NUM 26:1 (25-19) And it came to pass after the plague, (26-1) that the LORD
// spoke..." is Numbers 25:19 and 26:1. ----
var lines = File.ReadAllLines(args[0], Encoding.UTF8);
var words = new Dictionary<string, int>(StringComparer.Ordinal);   // how often each word is written each way, for fixing openings
foreach (var line in lines)
    foreach (Match w in Regex.Matches(line, @"[A-Za-z]+")) words[w.Value] = words.GetValueOrDefault(w.Value) + 1;

var hebrew = new Dictionary<string, SortedDictionary<(int, int), List<string>>>();
foreach (var line in lines)
{
    var m = Regex.Match(line, @"^(\S+) (\d+):(\d+) (.*)$");
    if (!m.Success || !codes.TryGetValue(m.Groups[1].Value, out var osis)) continue;
    var at = (int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value));
    string s = m.Groups[4].Value.Trim();
    if (at == (1, 1)) s = FixOpening(s, words);
    var book = hebrew.TryGetValue(osis, out var b) ? b : hebrew[osis] = [];
    // Split at the markers: text before the first belongs to the verse's own number.
    var pieces = Regex.Split(s, @"\((\d+)-(\d+)\)");          // text, chapter, verse, text, chapter, verse, text...
    for (int i = 0; i < pieces.Length; i += 3)
    {
        if (i > 0) at = (int.Parse(pieces[i - 2]), int.Parse(pieces[i - 1]));
        string piece = pieces[i].Trim();
        if (piece.Length == 0) continue;
        (book.TryGetValue(at, out var parts) ? parts : book[at] = []).Add(piece);   // several English verses may be one Hebrew verse
    }
}

// ---- Check against the Hebrew text and write ----
var text = new ScriptureText(data);
int problems = 0;
foreach (var book in Books.All)
{
    var verses = text.Range(book.Osis, new(1, 1), new(999, 999)).Select(x => (x.Chapter, x.Number)).ToHashSet();
    var mine = hebrew[book.Osis];
    foreach (var missing in verses.Where(x => !mine.ContainsKey(x)))
    { Console.WriteLine($"{book.Osis} {missing.Chapter}:{missing.Number}: no English"); problems++; }
    foreach (var extra in mine.Keys.Where(x => !verses.Contains(x)))
    { Console.WriteLine($"{book.Osis} {extra.Item1}:{extra.Item2}: no such Hebrew verse"); problems++; }
    foreach (var joined in mine.Where(kv => kv.Value.Count > 1))
        Console.WriteLine($"{book.Osis} {joined.Key.Item1}:{joined.Key.Item2}: {joined.Value.Count} English verses joined");

    Directory.CreateDirectory(Path.Combine(data, "English"));
    File.WriteAllLines(Path.Combine(data, "English", book.Osis + ".txt"),
        mine.Select(kv => $"{kv.Key.Item1}:{kv.Key.Item2} {NameStyle.Apply(string.Join(" ", kv.Value))}"), new UTF8Encoding(false));
}
Console.WriteLine(problems == 0 ? "OK: every Hebrew verse has exactly one English verse." : $"{problems} problems");
return problems == 0 ? 0 : 2;

/// <summary>A book's first words are printed in capitals ("AND MOAB rebelled"): give them their ordinary case,
/// as the same word is most often written elsewhere in the text ("And Moab rebelled"; "Now king David"; "the LORD" stays).</summary>
static string FixOpening(string s, Dictionary<string, int> words)
{
    var m = Regex.Match(s, @"^((?:[A-Z]{2,}|I|A)\b ?)+");
    int n = 0;
    string fixedWords = Regex.Replace(m.Value, @"[A-Z]+", w =>
    {
        string lower = w.Value.ToLowerInvariant(), title = char.ToUpperInvariant(lower[0]) + lower[1..];
        string result = n++ == 0 ? title
                      : w.Value == "LORD" || w.Value == "GOD" ? w.Value
                      : words.GetValueOrDefault(lower) > words.GetValueOrDefault(title) ? lower
                      : title;
        return result;
    });
    return fixedWords + s[m.Length..];
}

/// <summary>The divine name and "God" as Jewish readers often write them.</summary>
static class NameStyle
{
    public static string Apply(string s)
    {
        s = Regex.Replace(s, @"\bLORD'S\b", "LORD's");           // JPS prints the possessive in capitals
        s = Regex.Replace(s, @"\b[Tt]he LORD\b", "HaShem");      // the LORD, the LORD's
        s = Regex.Replace(s, @"\b(LORD|GOD)\b", "HaShem");       // "O LORD", "Lord GOD"
        s = Regex.Replace(s, @"\bGod\b", "G-d");                 // God, God's (not "gods")
        return s;
    }
}
