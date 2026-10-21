// Generates Data\MAM\sheva-na.json: which shevas in the app's MAM text are sounded, taken from Phonetic MAM's
// Sephardic transcriptions (a sounded sheva is transcribed as a superscript e). See Data\PhoneticMAM\SOURCE.md.
using System.Text;
using System.Text.Json;
using LeynerLearner.Core;

const char SE = '\u1D49';   // stands for {"kind":"superscript-e"} in flattened transcriptions
// usage: ShevaNaGenerator [app Data dir] [Phonetic-MAM data dir]
//   By default it reads the copy of Phonetic MAM bundled with the app (Data\PhoneticMAM\data).
string data = args.Length > 0 ? args[0] : Path.Combine("..", "..", "LeynerLearner", "Data");
string phon = args.Length > 1 ? args[1] : Path.Combine(data, "PhoneticMAM", "data");
var files = PhoneticMam.Files;

var text = new ScriptureText(data);
var output = new SortedDictionary<string, SortedDictionary<string, string>>();
var stats = new SortedDictionary<string, int>();
void Count(string k, int n = 1) => stats[k] = stats.GetValueOrDefault(k) + n;
var failures = new List<string>();

foreach (var (book, file) in files)
{
    // ---- Phonetic MAM words, in book order, with each word's sounded-sheva pattern ----
    var pwords = new List<(string Key, bool[]? Pattern, string ShevaLetters)>();
    var dict = new Dictionary<string, string?>();   // key -> pattern string, or null if inconsistent
    using (var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(phon, file + ".json"))))
        foreach (var ch in doc.RootElement.GetProperty("chapters").EnumerateArray())
            foreach (var v in ch.GetProperty("verses").EnumerateArray())
                foreach (var row in v.GetProperty("rows").EnumerateArray())
                {
                    string? heb = FirstWord(row.GetProperty("hebrew"));
                    var tr = row.GetProperty("transcriptions").GetProperty("sephardic");
                    string? translit = FirstTranslit(tr);
                    if (heb == null || translit == null) continue;
                    var pattern = Solve(heb, translit, out string why);
                    if (pattern == null) Count("phonetic words unresolved: " + why);
                    string key = Key(heb);
                    pwords.Add((key, pattern, ShevaLetters(heb)));
                    if (pattern != null)
                    {
                        string p = string.Concat(pattern.Select(b => b ? '1' : '0'));
                        dict[key] = dict.TryGetValue(key, out var old) && old != p ? null : p;
                    }
                }

    // ---- The app's words, in book order ----
    var verses = text.Range(book, new(1, 1), new(999, 999)).ToList();
    var mine = new List<(string VerseKey, int Word, string Token)>();
    foreach (var v in verses)
    {
        int w = 0;
        foreach (var t in v.Hebrew.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(ShevaNa.IsWord))
            mine.Add(($"{v.Chapter}:{v.Number}", w++, t));
    }

    // ---- Align in order (verse numbers differ between versifications; the words don't) ----
    var result = new Dictionary<string, List<string>>();
    void Record(string verseKey, int word, string token, bool[] pattern)
    {
        int shevas = token.Count(c => c == '\u05B0');
        Count("shevas", shevas);
        if (pattern.Length != shevas) { Count("shevas unmarked (count mismatch)", shevas); return; }
        for (int s = 0; s < pattern.Length; s++)
        {
            Count(pattern[s] ? "shevas sounded" : "shevas silent");
            if (pattern[s]) (result.TryGetValue(verseKey, out var l) ? l : result[verseKey] = []).Add($"{word}.{s}");
        }
    }
    void Unmatched(string verseKey, int word, string token)
    {
        // Fall back to the pattern of the same pointed word elsewhere in the book, if it is always the same.
        if (dict.TryGetValue(Key(token), out var p) && p != null) { Count("words matched by form"); Record(verseKey, word, token, p.Select(c => c == '1').ToArray()); }
        else
        {
            int shevas = token.Count(c => c == '\u05B0');
            Count("shevas", shevas);
            bool divineName = Letters(token).Contains("יהוה");
            Count(divineName ? "shevas unmarked (divine name)" : "shevas unmarked (no match)", shevas);
            if (shevas > 0 && !divineName && failures.Count < 60) failures.Add($"{book} {verseKey} {token}");
        }
    }

    int i = 0, j = 0;
    while (i < mine.Count)
    {
        var (vk, w, tok) = mine[i];
        string key = Key(tok);
        if (j < pwords.Count && pwords[j].Key == key)
        {
            if (pwords[j].Pattern is { } p) { Count("words matched in order"); Record(vk, w, tok, p); }
            else Unmatched(vk, w, tok);
            i++; j++;
            continue;
        }
        int skipP = Enumerable.Range(1, 8).FirstOrDefault(d => j + d < pwords.Count && pwords[j + d].Key == key);
        if (skipP > 0) { j += skipP; continue; }
        int skipM = Enumerable.Range(1, 8).FirstOrDefault(d => i + d < mine.Count && Key(mine[i + d].Token) == (j < pwords.Count ? pwords[j].Key : ""));
        if (skipM > 0) { for (int d = 0; d < skipM; d++) Unmatched(mine[i + d].VerseKey, mine[i + d].Word, mine[i + d].Token); i += skipM; continue; }
        // A word read differently from its spelling (e.g. הִוא read הִיא): use the pronounced form's pattern when its
        // shevas sit on the same letters.
        if (j < pwords.Count && pwords[j].Pattern is { } pp && ShevaLetters(tok) == pwords[j].ShevaLetters && pp.Length > 0)
        { Count("words matched by sheva letters"); Record(vk, w, tok, pp); }
        else Unmatched(vk, w, tok);
        i++; j++;
    }

    // ---- Upper cantillation (Ten Commandments, Gen 35:22): by form ----
    foreach (var v in verses.Where(v => v.HebrewUpper != null))
    {
        int w = 0;
        foreach (var t in v.HebrewUpper!.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(ShevaNa.IsWord))
        {
            if (dict.TryGetValue(Key(t), out var p) && p != null && p.Length == t.Count(c => c == '\u05B0'))
                for (int s = 0; s < p.Length; s++)
                    if (p[s] == '1') (result.TryGetValue($"{v.Chapter}:{v.Number}^", out var l) ? l : result[$"{v.Chapter}:{v.Number}^"] = []).Add($"{w}.{s}");
            w++;
        }
    }

    output[book] = new SortedDictionary<string, string>(result.ToDictionary(kv => kv.Key, kv => string.Join(" ", kv.Value)));
}

var opts = new JsonSerializerOptions { WriteIndented = false };
File.WriteAllText(Path.Combine(data, "MAM", "sheva-na.json"), JsonSerializer.Serialize(output, opts));
foreach (var (k, v) in stats) Console.WriteLine($"{k,-45} {v,8}");
File.WriteAllLines("sheva-failures.txt", failures);
return 0;

// ======================================================================================

// Pointed form used to match words: letters, vowels, dagesh, shin/sin dots, meteg (meteg changes the reading
// of a following sheva); no accents or other marks.
static string Key(string w) => new(w.Where(c => c is (>= '\u05D0' and <= '\u05EA') or (>= '\u05B0' and <= '\u05BD') or '\u05C1' or '\u05C2' or '\u05C7' or '\u05BE').ToArray());

// Hebrew letters only (no points, marks, spaces or maqaf).
static string Letters(string s) => new(s.Where(c => c is >= 'א' and <= 'ת').ToArray());

// The letters that carry a sheva, in order.
static string ShevaLetters(string w)
{
    var sb = new StringBuilder();
    char letter = ' ';
    foreach (char c in w)
    {
        if (c is >= '\u05D0' and <= '\u05EA') letter = c;
        else if (c == '\u05B0') sb.Append(letter);
    }
    return sb.ToString();
}

static string? FirstWord(JsonElement hebrew)
{
    foreach (var col in hebrew.EnumerateArray())
    {
        if (col.ValueKind != JsonValueKind.Array) continue;
        var first = col[0];
        if (first.ValueKind == JsonValueKind.String) return first.GetString();
        if (first.ValueKind == JsonValueKind.Object && first.GetProperty("kind").GetString() == "reading")
            return first.GetProperty("content")[0].GetString();
    }
    return null;
}

static string? FirstTranslit(JsonElement columns)
{
    foreach (var col in columns.EnumerateArray())
    {
        if (col.ValueKind != JsonValueKind.Array) continue;
        var items = col;
        if (items.GetArrayLength() > 0 && items[0].ValueKind == JsonValueKind.Object && items[0].GetProperty("kind").GetString() == "reading")
            items = items[0].GetProperty("content");
        var sb = new StringBuilder();
        Flatten(items, sb);
        return sb.ToString();
    }
    return null;
}

static void Flatten(JsonElement e, StringBuilder sb)
{
    if (e.ValueKind == JsonValueKind.String) sb.Append(e.GetString());
    else if (e.ValueKind == JsonValueKind.Array) foreach (var x in e.EnumerateArray()) Flatten(x, sb);
    else if (e.ValueKind == JsonValueKind.Object)
    {
        string kind = e.GetProperty("kind").GetString()!;
        if (kind == "superscript-e") sb.Append('\u1D49');
        else if (e.TryGetProperty("content", out var c)) Flatten(c, sb);
    }
}

/// <summary>
/// Which of a word's shevas are sounded. Hebrew full vowels must pair with transcribed syllables in order;
/// each sheva pairs either with a superscript-e syllable (sounded) or with nothing (silent). When several
/// pairings fit, the consonant written before the superscript e decides.
/// </summary>
static bool[]? Solve(string heb, string translit, out string why)
{
    // Hebrew: syllable nuclei per letter.
    var units = new List<(char Letter, char Kind)>();   // Kind: 'V' vowel, 'S' sheva
    char prevKind = ' ';
    char? prevLetter = null;
    for (int i = 0; i < heb.Length; i++)
    {
        char c = heb[i];
        if (c is < '\u05D0' or > '\u05EA') continue;
        int end = i + 1;
        while (end < heb.Length && heb[end] is not ((>= '\u05D0' and <= '\u05EA') or '\u05BE' or ' ')) end++;
        string marks = heb[(i + 1)..end];
        int vowels = marks.Count(m => m is (>= '\u05B1' and <= '\u05BB') or '\u05C7');
        bool sheva = marks.Contains('\u05B0');
        bool shuruq = c == 'ו' && vowels == 0 && !sheva && marks.Contains('\u05BC') && (prevLetter == null || prevKind == ' ');
        for (int k = 0; k < vowels; k++) units.Add((c, 'V'));
        if (shuruq) units.Add((c, 'V'));
        if (sheva) units.Add((c, 'S'));
        prevKind = vowels > 0 || sheva ? 'X' : ' ';
        prevLetter = c;
        i = end - 1;
    }

    // Transcription: syllables separated by "·" or "-".
    var nuclei = new List<(char Kind, string Before)>();
    foreach (var syl in translit.Split('·', '-'))
    {
        if (syl.Contains(SE)) nuclei.Add(('E', syl[..syl.IndexOf(SE)]));
        else if (syl.Normalize(NormalizationForm.FormD).Any(ch => "aeiouAEIOU".Contains(ch))) nuclei.Add(('V', ""));
    }

    var solutions = new List<bool[]>();
    void Search(int u, int n, List<bool> soFar, bool checkConsonant)
    {
        if (solutions.Count > 2) return;
        if (u == units.Count) { if (n == nuclei.Count) solutions.Add(soFar.ToArray()); return; }
        if (units[u].Kind == 'V')
        {
            if (n < nuclei.Count && nuclei[n].Kind == 'V') Search(u + 1, n + 1, soFar, checkConsonant);
            return;
        }
        if (n < nuclei.Count && nuclei[n].Kind == 'E' && (!checkConsonant || Compatible(units[u].Letter, nuclei[n].Before)))
        { soFar.Add(true); Search(u + 1, n + 1, soFar, checkConsonant); soFar.RemoveAt(soFar.Count - 1); }
        soFar.Add(false); Search(u + 1, n, soFar, checkConsonant); soFar.RemoveAt(soFar.Count - 1);
    }
    Search(0, 0, [], checkConsonant: true);
    if (solutions.Count == 0) Search(0, 0, [], checkConsonant: false);
    why = solutions.Count == 0 ? "no alignment" : solutions.Count > 1 ? "ambiguous" : "";
    return solutions.Count == 1 ? solutions[0] : null;
}

static bool Compatible(char letter, string before)
{
    if (before.Length == 0) return true;
    string[] options = letter switch
    {
        'א' => ["’"], 'ב' => ["b", "v"], 'ג' => ["g"], 'ד' => ["d"], 'ה' => ["h"], 'ו' => ["v", "w"], 'ז' => ["z"],
        'ח' => ["\u1E25", "h"], 'ט' => ["t", "\u1E6D"], 'י' => ["y"], 'כ' or 'ך' => ["k", "kh"], 'ל' => ["l"],
        'מ' or 'ם' => ["m"], 'נ' or 'ן' => ["n"], 'ס' => ["s"], 'ע' => ["‘"], 'פ' or 'ף' => ["p", "f"],
        'צ' or 'ץ' => ["ts", "\u1E93"], 'ק' => ["q", "k"], 'ר' => ["r"], 'ש' => ["sh", "s", "\u015B"], 'ת' => ["t", "th"],
        _ => [""],
    };
    string b = before.Normalize(NormalizationForm.FormC);
    // The whole consonant written before the superscript e must be this letter's ("sh" is not "h").
    return options.Any(o => b.Equals(o, StringComparison.OrdinalIgnoreCase));
}
