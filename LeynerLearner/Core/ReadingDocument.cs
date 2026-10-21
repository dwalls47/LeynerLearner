namespace LeynerLearner.Core;

public record ReadingSection(string Heading, string Range, List<Verse> Verses);

/// <summary>Everything needed to display or print one reading; shared by the screen and PDF renderers.</summary>
public record ReadingDocument(string Title, string Subtitle, List<ReadingSection> Sections, string Attribution)
{
    /// <summary>Required by the Hebrew text's CC BY-SA 4.0 license (see Data\MAM\README.md).</summary>
    public const string MamAttribution =
        "Hebrew text: Miqra according to the Masorah (Hebrew Wikisource), CC BY-SA 4.0 — " +
        "https://en.wikisource.org/wiki/User:Dovi/Miqra_according_to_the_Masorah";

    /// <param name="date">The day of the reading, or null for a reading chosen without a date.</param>
    /// <param name="upperTrope">Use the upper cantillation (ta'am elyon) where a verse has two.</param>
    /// <param name="shevaNa">When given, sounded shevas are marked in <paramref name="shevaStyle"/> (see <see cref="ShevaNa"/>).</param>
    /// <param name="phonetic">When given, each verse gets its words' transliterations.</param>
    public static ReadingDocument Build(Leyning leyning, ScriptureText text, DateTime? date, bool israel,
                                       bool upperTrope, HaftarahCustom custom,
                                       ShevaNa? shevaNa = null, ShevaNaStyle shevaStyle = ShevaNaStyle.Color,
                                       PhoneticMam? phonetic = null)
    {
        Verse Display(string book, Verse v)
        {
            bool upper = upperTrope && v.HebrewUpper != null;
            string hebrew = shevaNa != null ? shevaNa.Mark(book, v, upper, shevaStyle) : upper ? v.HebrewUpper! : v.Hebrew;
            return v with { Hebrew = hebrew, Phonetic = phonetic?.Words(book, v.Chapter, v.Number, upperTrope) };
        }

        ReadingSection Section(Aliyah a) => new(
            a.Note == null ? a.Label : $"{a.Label} ({a.Note})",
            a.RangeText,
            text.Range(a.Book, a.Start, a.End).Select(v => Display(a.Book, v)).ToList());

        var sections = leyning.Torah.Select(Section).ToList();
        foreach (var part in leyning.Haftarah)
        {
            var s = Section(part);
            string heading = string.IsNullOrEmpty(leyning.HaftarahLabel) ? s.Heading : $"{s.Heading} ({leyning.HaftarahLabel})";
            if (part == leyning.Haftarah[0] && leyning.HaftarahNote != null) heading += $" — {leyning.HaftarahNote}";
            sections.Add(s with { Heading = heading });
        }

        var sub = new List<string>();
        if (date is DateTime d)
        {
            sub.Add($"{d:dddd, MMMM d, yyyy}");
            sub.Add(ParashaCalendar.HebrewDate(d));
        }
        if (leyning.Special != null) sub.Add(leyning.Special);
        sub.Add(israel ? "Israel" : "Diaspora");
        return new ReadingDocument(leyning.Title, string.Join(" · ", sub), sections, MamAttribution);
    }
}
