using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LeynerLearner.Core;

/// <summary>
/// A verse: Hebrew (with the lower/"pshuta" cantillation where a verse has two), the upper
/// cantillation if it differs (Ten Commandments, Gen 35:22), English, and the paragraph mark after it.
/// </summary>
public record Verse(int Chapter, int Number, string Hebrew, string? HebrewUpper, string? English, string? ParagraphMark)
{
    /// <summary>Each word's transliteration (see <see cref="PhoneticMam"/>), when the reading has been built with it.</summary>
    public PhoneticWord?[]? Phonetic { get; init; }
}

/// <summary>
/// Torah and Prophets text: Hebrew from MAM (Miqra according to the Masorah, MAM-simple JSON, CC BY-SA 4.0
/// — see Data\MAM\README.md) and English from the JPS 1917 translation, in the same verse numbering (Data\English,
/// see its README.md).
///
/// The MAM files are chosen so verse numbers match Hebcal's (Leningrad-style) numbering; see the README.
/// </summary>
public partial class ScriptureText(string dataDir)
{
    readonly Dictionary<string, SortedDictionary<(int, int), Verse>> books = [];

    [GeneratedRegex(@"^(\d+):(\d+)\s+(.*)$")] private static partial Regex EnglishLine();
    [GeneratedRegex(@" {2,}")] private static partial Regex MultipleSpaces();

    /// <summary>All verses from start to end (inclusive) of a book.</summary>
    public IEnumerable<Verse> Range(string book, VerseRef start, VerseRef end) =>
        Load(book).Where(kv => kv.Key.CompareTo((start.Chapter, start.Verse)) >= 0
                            && kv.Key.CompareTo((end.Chapter, end.Verse)) <= 0)
                  .Select(kv => kv.Value);

    SortedDictionary<(int, int), Verse> Load(string book)
    {
        if (books.TryGetValue(book, out var cached)) return cached;

        var info = Books.Get(book);
        var english = new Dictionary<(int, int), string>();
        string engPath = Path.Combine(dataDir, "English", book + ".txt");
        if (File.Exists(engPath))
            foreach (var line in File.ReadAllLines(engPath, Encoding.UTF8))
                if (EnglishLine().Match(line) is { Success: true } m)
                    english[(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value))] = m.Groups[3].Value.Trim();

        var result = new SortedDictionary<(int, int), Verse>();
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dataDir, "MAM", info.MamFile + ".json")));
        foreach (var book39 in doc.RootElement.GetProperty("contents").EnumerateArray()
                     .Where(e => Type(e) == "book39" && e.GetProperty("osisID").GetString() == book))
            foreach (var chapter in book39.GetProperty("contents").EnumerateArray().Where(e => Type(e) == "chapter"))
                foreach (var verse in chapter.GetProperty("contents").EnumerateArray().Where(e => Type(e) == "verse"))
                {
                    var id = verse.GetProperty("osisID").GetString()!.Split('.');
                    int c = int.Parse(id[1]), v = int.Parse(id[2]);
                    string lower = VerseText(verse, upper: false), upper = VerseText(verse, upper: true);
                    string? mark = verse.TryGetProperty("ends-with-sampe", out var s) ? Sampe(s.GetString()!) : null;
                    // A few breaks sit inside the verse element at its very end (e.g. Lev 7:21); treat them the same.
                    if (mark == null && lower.Length > 2 && lower[^2] == ' ' && lower[^1] is 'פ' or 'ס' && upper.EndsWith(lower[^2..]))
                    {
                        mark = lower[^1..];
                        lower = lower[..^2];
                        upper = upper[..^2];
                    }
                    result[(c, v)] = new Verse(c, v, lower, upper == lower ? null : upper, english.GetValueOrDefault((c, v)), mark);
                }
        books[book] = result;
        return result;
    }

    static string Type(JsonElement e) => e.GetProperty("type").GetString()!;

    static string Sampe(string name) => name.StartsWith("pe") ? "פ" : "ס";

    static string VerseText(JsonElement verse, bool upper)
    {
        var sb = new StringBuilder();
        Append(sb, verse, upper);
        return MultipleSpaces().Replace(sb.ToString(), " ").Trim();
    }

    /// <summary>
    /// Appends an element's text. Closed dispatch, as MAM-simple asks: an unknown element type throws
    /// rather than silently dropping text.
    /// </summary>
    static void Append(StringBuilder sb, JsonElement e, bool upper)
    {
        switch (Type(e))
        {
            // Containers whose text is their text attribute or their children, in order.
            case "verse" or "text" or "kq-q" or "kq-trivial" or "kq-q-velo-k"
                 or "letter-small" or "letter-large" or "letter-hung" or "sdt-target"
                 or "cant-alef" or "cant-bet":
                if (e.TryGetProperty("text", out var t)) sb.Append(t.GetString());
                else AppendChildren(sb, e, upper);
                break;
            case "slh-word":                 // word with a large/small/hung letter: spelled by its children
                AppendChildren(sb, e, upper);  // (slhw-desc-0 repeats the same word; don't add both)
                break;

            // Alternatives: choose one branch.
            case "kq":                       // the qere is what is chanted
                Append(sb, Child(e, "kq-q"), upper);
                break;
            case "cant-all-three":           // lower (tachton / pshuta) or upper (elyon / midrashit)
                Append(sb, Child(e, upper ? "cant-bet" : "cant-alef"), upper);
                break;
            case "scrdfftar":                // the text, not the note about scroll differences
                Append(sb, Child(e, "sdt-target"), upper);
                break;

            // Not read aloud / not running text.
            case "kq-k" or "kq-k-velo-q" or "kq-k-velo-q-maq" or "sdt-note" or "good-ending" or "cant-combined":
                break;

            // Marks.
            case "lp-legarmeih" or "lp-paseq":
                sb.Append(" ׀ ");       // belongs to the preceding word; spaced for display
                break;
            case "implicit-maqaf":
                sb.Append('־');
                break;
            case "shirah-space":
                sb.Append("   ");       // song layout (Exod 15, Deut 32)
                break;
            case "spi-invnun":
                sb.Append(" ׆ ");       // inverted nun (Num 10:35-36)
                break;
            case "spi-pe2" or "spi-pe3":     // paragraph break inside a verse (e.g. Gen 35:22)
                sb.Append(" פ ");
                break;
            case "spi-samekh2" or "spi-samekh3":
                sb.Append(" ס ");
                break;

            default:
                throw new InvalidDataException($"Unknown MAM element type '{Type(e)}'");
        }
    }

    static void AppendChildren(StringBuilder sb, JsonElement e, bool upper)
    {
        if (e.TryGetProperty("contents", out var children))
            foreach (var child in children.EnumerateArray()) Append(sb, child, upper);
    }

    static JsonElement Child(JsonElement e, string type) =>
        e.GetProperty("contents").EnumerateArray().First(c => Type(c) == type);
}
