using System.Text;
using System.Text.Json;

namespace LeynerLearner.Core;

/// <summary>How sounded shevas are shown.</summary>
public enum ShevaNaStyle
{
    Off,
    /// <summary>The letter carrying the sounded sheva is drawn in color (any font).</summary>
    Color,
    /// <summary>A larger sheva glyph, from the bundled "Hebrew Sheva Na" font (other fonts show a normal sheva).</summary>
    LargeSheva,
    /// <summary>Both: the large sheva, and its letter in color.</summary>
    LargeShevaAndColor,
    /// <summary>A rafe (the bar above the letter, U+05BF) added to the letter (any font that has the rafe).</summary>
    ShevaRafe,
    /// <summary>Both: the rafe, and the letter in color.</summary>
    ShevaRafeAndColor,
}

/// <summary>
/// Which shevas are sounded (sheva na), from Phonetic MAM's transcriptions (Data\MAM\sheva-na.json, generated
/// by aligning the transcriptions with this text; see Data\MAM\README.md).
///
/// A sounded sheva is shown by putting <see cref="Marker"/> before the letter that carries it; renderers draw
/// that letter (with its marks) in a distinct color and never draw the marker itself.
/// </summary>
public class ShevaNa
{
    /// <summary>Private-use character placed before a letter whose sheva is sounded (Color styles).</summary>
    public const char Marker = '';

    /// <summary>COMBINING GRAPHEME JOINER, placed right after a sounded sheva (LargeSheva styles). The bundled
    /// font substitutes its large sheva for sheva + CGJ; other fonts ignore the CGJ, and copied text stays
    /// ordinary Unicode.</summary>
    public const char Joiner = '͏';

    /// <summary>HEBREW POINT RAFE, added to a letter with a sounded sheva (ShevaRafe styles).</summary>
    public const char Rafe = 'ֿ';

    const char Dagesh = 'ּ';

    /// <summary>Family name of the bundled font (Data\Fonts\HebrewShevaNa.ttf).</summary>
    public const string FontFamily = "Hebrew Sheva Na";

    const char Sheva = 'ְ';

    // "Gen 1:1" (or "Exod 20:13^" for the upper cantillation) -> (word index, sheva index within the word)
    readonly Dictionary<string, List<(int Word, int Sheva)>> verses = [];

    public static ShevaNa Load(string path)
    {
        var s = new ShevaNa();
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        foreach (var book in doc.RootElement.EnumerateObject())
            foreach (var verse in book.Value.EnumerateObject())
                s.verses[$"{book.Name} {verse.Name}"] = verse.Value.GetString()!.Split(' ')
                    .Select(p => p.Split('.')).Select(p => (int.Parse(p[0]), int.Parse(p[1]))).ToList();
        return s;
    }

    public static string Key(string book, int chapter, int verse, bool upper) => $"{book} {chapter}:{verse}{(upper ? "^" : "")}";

    /// <summary>The verse text with its sounded shevas marked in the given style.</summary>
    public string Mark(string book, Verse v, bool upper, ShevaNaStyle style = ShevaNaStyle.Color)
    {
        string hebrew = upper && v.HebrewUpper != null ? v.HebrewUpper : v.Hebrew;
        if (style == ShevaNaStyle.Off) return hebrew;
        return verses.TryGetValue(Key(book, v.Chapter, v.Number, upper && v.HebrewUpper != null), out var list)
            ? Mark(hebrew, list, style) : hebrew;
    }

    /// <summary>The words that shevas are counted in: space-separated, maqaf-joined words kept together,
    /// with paragraph markers, paseq, song gaps and the inverted nun left out.</summary>
    public static bool IsWord(string token) => token is not ("פ" or "ס" or "׀" or " " or "׆");

    public static string Mark(string hebrew, IReadOnlyCollection<(int Word, int Sheva)> vocal,
                              ShevaNaStyle style = ShevaNaStyle.Color)
    {
        var tokens = hebrew.Split(' ');
        int word = -1;
        for (int t = 0; t < tokens.Length; t++)
        {
            if (tokens[t].Length == 0 || !IsWord(tokens[t])) continue;
            word++;
            var shevas = vocal.Where(x => x.Word == word).Select(x => x.Sheva).ToHashSet();
            if (shevas.Count == 0) continue;
            var sb = new StringBuilder(tokens[t]);
            int count = 0;
            for (int i = 0; i < sb.Length; i++)
            {
                if (sb[i] != Sheva) continue;
                if (!shevas.Contains(count++)) continue;
                if (style is ShevaNaStyle.LargeSheva or ShevaNaStyle.LargeShevaAndColor)
                {
                    sb.Insert(i + 1, Joiner);
                    i++;
                }
                if (style is ShevaNaStyle.ShevaRafe or ShevaNaStyle.ShevaRafeAndColor)
                {
                    // After the sheva and a dagesh that follows it: the points' canonical order.
                    int at = i + 1;
                    if (at < sb.Length && sb[at] == Dagesh) at++;
                    sb.Insert(at, Rafe);
                    i = at;
                }
                if (style is ShevaNaStyle.Color or ShevaNaStyle.LargeShevaAndColor or ShevaNaStyle.ShevaRafeAndColor)
                {
                    // The colored run is the marker, the letter and all its marks (the joiner too, so the font
                    // still sees sheva + joiner together).
                    int letter = i;
                    while (letter > 0 && !IsLetter(sb[letter])) letter--;
                    sb.Insert(letter, Marker);
                    i++;
                }
            }
            tokens[t] = sb.ToString();
        }
        return string.Join(' ', tokens);
    }

    public static bool IsLetter(char c) => c is >= 'א' and <= 'ת';

    /// <summary>Splits text into runs: (text, sounded) where a sounded run is one letter with its marks.</summary>
    public static IEnumerable<(string Text, bool Sounded)> Runs(string text)
    {
        var plain = new StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != Marker) { plain.Append(text[i]); continue; }
            if (plain.Length > 0) { yield return (plain.ToString(), false); plain.Clear(); }
            int end = i + 1;
            if (end < text.Length) end++;                                           // the letter
            while (end < text.Length && IsMark(text[end])) end++;                   // its points and accents
            yield return (text[(i + 1)..end], true);
            i = end - 1;
        }
        if (plain.Length > 0) yield return (plain.ToString(), false);
    }

    static bool IsMark(char c) => c is (>= '֑' and <= 'ֽ') or 'ֿ' or 'ׁ' or 'ׂ' or 'ׄ' or 'ׅ' or 'ׇ' or 'ﬞ' or '͏';
}
