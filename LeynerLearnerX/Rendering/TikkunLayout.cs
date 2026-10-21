using LeynerLearner.Core;

namespace LeynerLearner.Rendering;

/// <summary>A word, or a fixed gap (setumah), on a line of prose.</summary>
public record TikkunItem(string? Word, float Gap = 0);

public abstract record TikkunLine;

/// <summary>A line of prose; justified to both margins, or (before a petuchah / at the end) flush right.</summary>
public record ProseLine(List<TikkunItem> Items, bool Justify) : TikkunLine;

/// <summary>A line of a song: cells laid out right, (center,) left, with the width shares from the layout.</summary>
public record SongLine(List<(string Text, string Align, float Weight)> Cells) : TikkunLine;

public record BlankLine : TikkunLine;

/// <summary>
/// Lays out Torah text the way a scroll is written: fixed-width justified lines with no verse breaks,
/// petuchah (the rest of the line left open) and setumah (a gap within the line) paragraph breaks, and
/// the traditional layouts of the Song of the Sea and Ha'azinu. Line breaks are computed, so they won't
/// match a particular scroll line for line.
/// </summary>
public class TikkunLayout
{
    /// <summary>A scroll line holds about this many letters.</summary>
    public const int LettersPerLine = 34;

    /// <summary>Petuchah and setumah gaps are the width of nine letters.</summary>
    const int GapLetters = 9;

    readonly Func<string, float> measure;
    readonly IReadOnlyList<Song> songs;
    readonly bool vowels, trope;
    readonly float spaceWidth, letterWidth;

    public float ColumnWidth { get; }

    /// <param name="measure">Width of a string in points in the chosen font and size.</param>
    /// <param name="maxWidth">Available width (the page's text width).</param>
    public TikkunLayout(Func<string, float> measure, float maxWidth, float size, IReadOnlyList<Song> songs,
                        bool vowels, bool trope)
    {
        this.measure = measure;
        this.songs = songs;
        this.vowels = vowels;
        this.trope = trope;
        letterWidth = measure("אבגדהוזחטיכלמנסעפצקרשת") / 22;
        spaceWidth = size * 0.3f;
        // As in a scroll, the column is wide enough for the songs' longest lines; otherwise ~34 letters.
        float widestSongLine = songs.SelectMany(s => s.Rows).Where(r => r.Cells.Count > 1)
            .Select(r => r.Cells.Sum(c => measure(Show(string.Join(" ", c.Text)))) + (r.Cells.Count - 1) * 3 * letterWidth)
            .DefaultIfEmpty(0).Max();
        // 5% allowance for differences between this measurement and the PDF's text shaping.
        ColumnWidth = Math.Min(maxWidth, Math.Max(letterWidth * LettersPerLine, widestSongLine * 1.05f));
    }

    /// <summary>Text of a word as shown: vowels and trope removed unless wanted (as in a scroll).</summary>
    public string Show(string word)
    {
        var sb = new System.Text.StringBuilder(word.Length);
        foreach (char c in word)
        {
            bool keep = c switch
            {
                >= '֑' and <= '֯' => trope,                     // cantillation marks
                'ֽ' or '׃' or '׀' => trope,                // meteg, sof pasuq, paseq
                >= 'ְ' and <= 'ּ' => vowels,                    // vowels, dagesh
                'ֿ' or 'ׁ' or 'ׂ' or 'ׇ' => vowels,   // rafe, shin/sin dots, qamats qatan
                '־' => vowels || trope,                              // maqaf (a scroll writes the words apart)
                'ﬞ' => vowels,                                       // varika
                ShevaNa.Marker => vowels,                                 // sounded-sheva color (shevas are vowels)
                '͏' => vowels,                                       // CGJ: orders points / selects the large sheva
                _ => true,                                                // letters, puncta, inverted nun
            };
            if (keep) sb.Append(c);
        }
        return sb.ToString();
    }

    public List<TikkunLine> Layout(ReadingSection section)
    {
        var lines = new List<TikkunLine>();
        var items = new List<TikkunItem>();
        float width = 0;          // width used on the current line, including minimum spaces
        var inSection = section.Verses.Select(v => (v.Chapter, v.Number)).ToHashSet();
        var verses = section.Verses.ToDictionary(v => (v.Chapter, v.Number));
        var songsDone = new HashSet<Song>();

        void Emit(bool justify)
        {
            if (items.Count == 0) return;
            lines.Add(new ProseLine(items, justify && items.Count(i => i.Word != null) > 1));
            items = [];
            width = 0;
        }

        void AddWord(string word)
        {
            float w = measure(word);
            float needed = items.Count == 0 ? w : width + (items[^1].Word != null ? spaceWidth : 0) + w;
            if (items.Count > 0 && needed > ColumnWidth * 0.95f)
            {
                Emit(justify: true);
                needed = w;
            }
            items.Add(new TikkunItem(word));
            width = needed;
        }

        void Petuchah()
        {
            if (items.Count == 0) { lines.Add(new BlankLine()); return; }   // previous line was full
            bool roomLeft = ColumnWidth - width >= GapLetters * letterWidth;
            Emit(justify: false);
            if (!roomLeft) lines.Add(new BlankLine());
        }

        void Setumah()
        {
            float gap = GapLetters * letterWidth;
            if (items.Count > 0 && width + gap + 3 * letterWidth > ColumnWidth * 0.95f) Emit(justify: true);
            items.Add(new TikkunItem(null, gap));
            width += gap;
        }

        foreach (var v in section.Verses)
        {
            var song = songs.FirstOrDefault(s => s.Contains(section.Book, v.Chapter, v.Number));
            if (song != null)
            {
                if (songsDone.Add(song)) { Emit(justify: false); AddSong(song, inSection, verses, lines); }
                if (v.ParagraphMark == "פ") lines.Add(new BlankLine());
                continue;
            }

            string? pending = null;   // a word ending in maqaf stays with the next word
            foreach (var token in v.Hebrew.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (token is "פ") { Petuchah(); continue; }
                if (token is "ס") { Setumah(); continue; }
                if (token is " ") continue;
                if (token is "׀")
                {
                    if (trope && items.Count > 0 && items[^1].Word is string prev) items[^1] = new TikkunItem(prev + " ׀");
                    continue;
                }
                foreach (var part in token.Replace("־", "־ ").Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    string shown = Show(part);
                    if (shown.Length == 0) continue;
                    if (shown.EndsWith('־')) { pending = (pending ?? "") + shown; continue; }
                    AddWord((pending ?? "") + shown);
                    pending = null;
                }
            }
            if (pending != null) AddWord(pending);
            if (v.ParagraphMark == "פ") Petuchah();
            else if (v.ParagraphMark == "ס") Setumah();
        }
        Emit(justify: false);
        return lines;
    }

    /// <summary>The song's lines that have words in this section (partial lines keep their cell positions).</summary>
    void AddSong(Song song, HashSet<(int, int)> inSection, Dictionary<(int, int), Verse> verses, List<TikkunLine> lines)
    {
        var words = new Dictionary<(int, int), List<string>>();
        string Word(WordRef w)
        {
            if (!words.TryGetValue((w.Chapter, w.Verse), out var list))
                words[(w.Chapter, w.Verse)] = list = Songs.Words(verses[(w.Chapter, w.Verse)].Hebrew);
            return Show(list[w.Index]);
        }
        string Join(IEnumerable<string> ws) =>
            string.Concat(ws.Select((w, i) => i == 0 || ws.ElementAt(i - 1).EndsWith('־') ? w : " " + w));

        foreach (var row in song.Rows)
        {
            var cells = row.Cells.Select(c => Join(c.Words.Where(w => inSection.Contains((w.Chapter, w.Verse))).Select(Word))).ToList();
            if (cells.All(c => c.Length == 0)) continue;
            if (row.Cells.Count == 1)
            {
                // A full line of the song (the opening line of the Song of the Sea): justified prose.
                lines.Add(new ProseLine(cells[0].Split(' ').Select(w => new TikkunItem(w)).ToList(), Justify: true));
                continue;
            }
            lines.Add(new SongLine(row.Cells.Select((c, i) => (cells[i], c.Align, c.Width > 0 ? c.Width : 100f / row.Cells.Count)).ToList()));
        }
    }
}
