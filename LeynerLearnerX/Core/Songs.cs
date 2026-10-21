using System.Text.Json;

namespace LeynerLearner.Core;

/// <summary>A word of a verse: its chapter, verse and index among the verse's words (see <see cref="Songs.Words"/>).</summary>
public record WordRef(int Chapter, int Verse, int Index);

/// <summary>One cell of a song line: "right", "center" or "left", its share of the width (0 = equal), its words
/// (as references into the verses) and their text.</summary>
public record SongCell(string Align, int Width, List<WordRef> Words, List<string> Text);

public record SongRow(List<SongCell> Cells);

/// <summary>A song written in a special layout in Torah scrolls.</summary>
public record Song(string Book, VerseRef First, VerseRef Last, List<SongRow> Rows)
{
    public bool Contains(string book, int chapter, int verse) =>
        book == Book && (chapter, verse).CompareTo((First.Chapter, First.Verse)) >= 0
                     && (chapter, verse).CompareTo((Last.Chapter, Last.Verse)) <= 0;
}

/// <summary>
/// The Song of the Sea and Ha'azinu as laid out in Ashkenazi/Sephardi Torah scrolls (Data\MAM\songs.json,
/// from MAM's Hebrew Wikisource pages). The layout's own text is used only to find where each cell starts
/// and ends in the verse text: cells are matched to verse words by their consonants.
/// </summary>
public static class Songs
{
    /// <summary>The words of a verse, as a Torah scroll writes them: split at spaces and after each maqaf
    /// (which keeps its place at the end of the first word), with paragraph markers, song gaps and
    /// paseq/legarmeh marks left out.</summary>
    public static List<string> Words(string hebrew) =>
        hebrew.Replace("־", "־ ")
              .Split(' ', StringSplitOptions.RemoveEmptyEntries)
              .Where(w => w is not ("פ" or "ס" or "׀" or " "))
              .ToList();

    public static List<Song> Load(string path, ScriptureText text)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var songs = new List<Song>();
        foreach (var (key, first, last) in new[] { ("sea", "15:1", "15:19"), ("haazinu", "32:1", "32:43") })
        {
            var s = doc.RootElement.GetProperty(key);
            string book = s.GetProperty("book").GetString()!;
            VerseRef f = VerseRef.Parse(first), l = VerseRef.Parse(last);

            // The song's words in order, with their consonants.
            var stream = text.Range(book, f, l)
                .SelectMany(v => Words(v.Hebrew).Select((w, i) => (Ref: new WordRef(v.Chapter, v.Number, i), Word: w, Letters: Letters(w))))
                .ToList();

            int pos = 0;
            var rows = new List<SongRow>();
            foreach (var row in s.GetProperty("rows").EnumerateArray())
            {
                var cells = new List<SongCell>();
                foreach (var cell in row.EnumerateArray())
                {
                    string target = Letters(cell.GetProperty("text").GetString()!), got = "";
                    var words = new List<WordRef>();
                    var texts = new List<string>();
                    while (got.Length < target.Length && pos < stream.Count)
                    {
                        words.Add(stream[pos].Ref);
                        texts.Add(stream[pos].Word);
                        got += stream[pos++].Letters;
                    }
                    if (got != target)
                        throw new InvalidDataException($"Song layout '{key}' does not match the text near {words.LastOrDefault()}");
                    cells.Add(new SongCell(cell.GetProperty("align").GetString()!, cell.GetProperty("width").GetInt32(), words, texts));
                }
                rows.Add(new SongRow(cells));
            }
            if (pos != stream.Count) throw new InvalidDataException($"Song layout '{key}' leaves {stream.Count - pos} words unplaced");
            songs.Add(new Song(book, f, l, rows));
        }
        return songs;
    }

    /// <summary>Hebrew letters only (no points, marks, spaces or maqaf).</summary>
    public static string Letters(string s) => new(s.Where(c => c is >= 'א' and <= 'ת').ToArray());
}
