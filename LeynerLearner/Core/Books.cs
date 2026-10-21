namespace LeynerLearner.Core;

/// <summary>
/// The books the app can display (Torah and the Prophets used for haftarot).
/// Books are identified by OSIS id ("Gen", "1Sam", ...), as in MAM.
/// </summary>
public static class Books
{
    /// <param name="Osis">OSIS id, as used by MAM</param>
    /// <param name="English">Display name</param>
    /// <param name="Hebcal">Name used in Hebcal's data</param>
    /// <param name="MamFile">MAM-simple book-group file (Data\MAM\*.json)</param>
    public record Book(string Osis, string English, string Hebcal, string MamFile);

    public static readonly Book[] All =
    [
        new("Gen", "Genesis", "Genesis", "Gen"),
        new("Exod", "Exodus", "Exodus", "Exod"),
        new("Lev", "Leviticus", "Leviticus", "Lev"),
        new("Num", "Numbers", "Numbers", "Num"),
        new("Deut", "Deuteronomy", "Deuteronomy", "Deut"),
        new("Josh", "Joshua", "Joshua", "Josh"),
        new("Judg", "Judges", "Judges", "Judg"),
        new("1Sam", "1 Samuel", "I Samuel", "1Sam-2Sam"),
        new("2Sam", "2 Samuel", "II Samuel", "1Sam-2Sam"),
        new("1Kgs", "1 Kings", "I Kings", "1Kgs-2Kgs"),
        new("2Kgs", "2 Kings", "II Kings", "1Kgs-2Kgs"),
        new("Isa", "Isaiah", "Isaiah", "Isa"),
        new("Jer", "Jeremiah", "Jeremiah", "Jer"),
        new("Ezek", "Ezekiel", "Ezekiel", "Ezek"),
        new("Hos", "Hosea", "Hosea", "Hos-Mal"),
        new("Joel", "Joel", "Joel", "Hos-Mal"),
        new("Amos", "Amos", "Amos", "Hos-Mal"),
        new("Obad", "Obadiah", "Obadiah", "Hos-Mal"),
        new("Jonah", "Jonah", "Jonah", "Hos-Mal"),
        new("Mic", "Micah", "Micah", "Hos-Mal"),
        new("Nah", "Nahum", "Nachum", "Hos-Mal"),
        new("Hab", "Habakkuk", "Habakkuk", "Hos-Mal"),
        new("Zeph", "Zephaniah", "Zephaniah", "Hos-Mal"),
        new("Hag", "Haggai", "Haggai", "Hos-Mal"),
        new("Zech", "Zechariah", "Zechariah", "Hos-Mal"),
        new("Mal", "Malachi", "Malachi", "Hos-Mal"),
    ];

    static readonly Dictionary<string, Book> ByOsis = All.ToDictionary(b => b.Osis);
    static readonly Dictionary<string, Book> ByHebcal = All.ToDictionary(b => b.Hebcal);

    public static Book Get(string osis) => ByOsis[osis];

    public static string EnglishName(string osis) => ByOsis[osis].English;

    /// <summary>Book named in Hebcal data: a Torah book number (1-5) or a name like "II Kings".</summary>
    public static string FromHebcal(string nameOrNumber) =>
        int.TryParse(nameOrNumber, out int n) ? All[n - 1].Osis : ByHebcal[nameOrNumber].Osis;
}
