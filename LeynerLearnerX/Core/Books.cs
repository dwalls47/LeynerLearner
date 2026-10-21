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
    /// <param name="Hnv">TropeTrainer's English file (Data\HNV\*.eng), if there is one</param>
    public record Book(string Osis, string English, string Hebcal, string MamFile, string? Hnv);

    public static readonly Book[] All =
    [
        new("Gen", "Genesis", "Genesis", "Gen", "gen"),
        new("Exod", "Exodus", "Exodus", "Exod", "exo"),
        new("Lev", "Leviticus", "Leviticus", "Lev", "lev"),
        new("Num", "Numbers", "Numbers", "Num", "num"),
        new("Deut", "Deuteronomy", "Deuteronomy", "Deut", "deu"),
        new("Josh", "Joshua", "Joshua", "Josh", "jos"),
        new("Judg", "Judges", "Judges", "Judg", "jud"),
        new("1Sam", "1 Samuel", "I Samuel", "1Sam-2Sam", "1sa"),
        new("2Sam", "2 Samuel", "II Samuel", "1Sam-2Sam", "2sa"),
        new("1Kgs", "1 Kings", "I Kings", "1Kgs-2Kgs", "1ki"),
        new("2Kgs", "2 Kings", "II Kings", "1Kgs-2Kgs", "2ki"),
        new("Isa", "Isaiah", "Isaiah", "Isa", "isa"),
        new("Jer", "Jeremiah", "Jeremiah", "Jer", "jer"),
        new("Ezek", "Ezekiel", "Ezekiel", "Ezek", "eze"),
        new("Hos", "Hosea", "Hosea", "Hos-Mal", "hos"),
        new("Joel", "Joel", "Joel", "Hos-Mal", "joe"),
        new("Amos", "Amos", "Amos", "Hos-Mal", "amo"),
        new("Obad", "Obadiah", "Obadiah", "Hos-Mal", "oba"),
        new("Jonah", "Jonah", "Jonah", "Hos-Mal", "jon"),
        new("Mic", "Micah", "Micah", "Hos-Mal", "mic"),
        new("Nah", "Nahum", "Nachum", "Hos-Mal", null),
        new("Hab", "Habakkuk", "Habakkuk", "Hos-Mal", "hab"),
        new("Zeph", "Zephaniah", "Zephaniah", "Hos-Mal", null),
        new("Hag", "Haggai", "Haggai", "Hos-Mal", null),
        new("Zech", "Zechariah", "Zechariah", "Hos-Mal", "zec"),
        new("Mal", "Malachi", "Malachi", "Hos-Mal", "mal"),
    ];

    static readonly Dictionary<string, Book> ByOsis = All.ToDictionary(b => b.Osis);
    static readonly Dictionary<string, Book> ByHebcal = All.ToDictionary(b => b.Hebcal);

    public static Book Get(string osis) => ByOsis[osis];

    public static string EnglishName(string osis) => ByOsis[osis].English;

    /// <summary>Book named in Hebcal data: a Torah book number (1-5) or a name like "II Kings".</summary>
    public static string FromHebcal(string nameOrNumber) =>
        int.TryParse(nameOrNumber, out int n) ? All[n - 1].Osis : ByHebcal[nameOrNumber].Osis;
}
