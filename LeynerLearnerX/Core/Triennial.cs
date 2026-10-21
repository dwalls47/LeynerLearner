using System.Text.Json;

namespace LeynerLearner.Core;

public record VerseRef(int Chapter, int Verse)
{
    /// <summary>Parses "12:3". A half-verse suffix ("2:2b", "9:5a" in a few triennial haftarot) is dropped:
    /// the whole verse is shown.</summary>
    public static VerseRef Parse(string s)
    {
        var p = s.Split(':');
        return new(int.Parse(p[0]), int.Parse(p[1].TrimEnd('a', 'b')));
    }
    public override string ToString() => $"{Chapter}:{Verse}";
}

/// <summary>One aliyah or haftarah part: a verse range in a book (OSIS id, e.g. "Gen", "Isa").</summary>
public record Aliyah(string Label, string Book, VerseRef Start, VerseRef End, string? Note = null)
{
    public string RangeText => Start.Chapter == End.Chapter
        ? $"{Books.EnglishName(Book)} {Start}-{End.Verse}"
        : $"{Books.EnglishName(Book)} {Start}-{End}";

    public static string LabelFor(string n) => n switch
    {
        "1" => "Rishon", "2" => "Sheni", "3" => "Shlishi", "4" => "R'vi'i",
        "5" => "Chamishi", "6" => "Shishi", "7" => "Sh'vi'i", "M" => "Maftir", _ => $"Aliyah {n}",
    };
}

public record TriennialReading(string Parasha, int CycleYear, string Variation, List<Aliyah> Aliyot);

/// <summary>
/// Triennial Torah readings (the Rabbinical Assembly's triennial cycle), using the aliyot table and
/// the selection logic from Hebcal's @hebcal/triennial (BSD-2-Clause, see Data\Hebcal\README.md).
///
/// Portions that are sometimes combined (Vayakhel-Pekudei etc.) have different readings depending on
/// whether the pair is read [T]ogether or [S]eparately in each year of the 3-year cycle; that
/// pattern selects a variation ("A".."G", "IL..") from the table.
/// </summary>
public class Triennial
{
    /// <summary>5744 was the first year of a triennial cycle.</summary>
    const int Epoch = 5744;

    record Range(string Book, VerseRef Start, VerseRef End);

    class Parsha
    {
        public required string Book;
        public Dictionary<string, Dictionary<string, Range>> Variations = [];
        public Dictionary<string, string> Patterns = [];
    }

    readonly Dictionary<string, Parsha> parshiyot = [];

    public static int CycleYear(int readingYear) => (readingYear - Epoch) % 3 + 1;

    public static Triennial Load(string path)
    {
        var t = new Triennial();
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        foreach (var p in doc.RootElement.EnumerateObject())
        {
            var parsha = new Parsha { Book = Books.All[p.Value.GetProperty("book").GetInt32() - 1].Osis };
            var vars = p.Value.TryGetProperty("years", out var y) ? y : p.Value.GetProperty("variations");
            var sameAs = new Dictionary<string, string>();
            foreach (var v in vars.EnumerateObject())
            {
                if (v.Value.ValueKind == JsonValueKind.String) { sameAs[v.Name] = v.Value.GetString()!; continue; }
                parsha.Variations[v.Name] = v.Value.EnumerateObject().ToDictionary(
                    a => a.Name,
                    a => new Range(parsha.Book, VerseRef.Parse(a.Value[0].GetString()!), VerseRef.Parse(a.Value[1].GetString()!)));
            }
            foreach (var (name, target) in sameAs) parsha.Variations[name] = parsha.Variations[target];
            if (p.Value.TryGetProperty("patterns", out var pat))
                foreach (var x in pat.EnumerateObject()) parsha.Patterns[x.Name] = x.Value.GetString()!;
            t.parshiyot[p.Name] = parsha;
        }
        return t;
    }

    /// <summary>Readings for a weekly portion (one, or a combined pair) in a given reading year.</summary>
    public TriennialReading Get(int[] portions, int readingYear, bool israel)
    {
        int year = CycleYear(readingYear);
        string name = ParashaCalendar.Name(portions);

        // A combined reading always uses its own Y.1-Y.3 table; only separately-read halves vary.
        string key = "Y";
        int pair = Array.Find(ParashaCalendar.SometimesCombined, d => d == portions[0] || d == portions[0] - 1);
        if (pair != 0 && portions.Length == 1)
        {
            string pattern = ThreeYearPattern(pair, readingYear - (year - 1), israel);
            if (pattern != "TTT") key = parshiyot[ParashaCalendar.Name([pair, pair + 1])].Patterns[pattern];
        }
        string variation = $"{key}.{year}";
        var ranges = parshiyot[name].Variations[variation];

        var aliyot = new List<Aliyah>();
        foreach (var n in new[] { "1", "2", "3", "4", "5", "6", "7", "M" })
            if (ranges.TryGetValue(n, out var r)) aliyot.Add(new Aliyah(Aliyah.LabelFor(n), r.Book, r.Start, r.End));
        return new TriennialReading(name, year, variation, aliyot);
    }

    /// <summary>T/S for each year of the cycle: is the pair read together or separately?</summary>
    static string ThreeYearPattern(int pair, int cycleStart, bool israel)
    {
        var chars = new char[3];
        for (int yr = 0; yr < 3; yr++)
            chars[yr] = ParashaCalendar.ForReadingYear(cycleStart + yr, israel)
                .Any(r => r.Portions.Length == 2 && r.Portions[0] == pair) ? 'T' : 'S';
        return new string(chars);
    }
}
