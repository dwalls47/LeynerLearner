// Generates Data\EnglishModern\<book>.txt, "JPS 1917 Modern": the JPS 1917 English of Data\English in modern English.
// Rules, many following Adam Cohn's "Modernized Tanakh" (Genesis and Jonah, on Sefaria, CC-BY): thee/thou/ye -> you,
// thy -> your, -eth and -est verbs, obsolete words and idioms (unto, hath, thereof, came to pass, I pray thee, behold),
// shall -> will except when addressed to "you", British -our spellings. The translation's wording is otherwise kept.
// Writes, in the working folder: changes.txt (every change, with counts; what may be left; samples) and
// needs-polish.txt (verses with constructions the rules don't handle, such as "knew not", for a final light edit).
using System.Text;
using System.Text.RegularExpressions;

// usage: EnglishModernizer [app Data dir]
string data = args.Length > 0 ? args[0] : Path.Combine("..", "..", "LeynerLearner", "Data");
string src = Path.Combine(data, "English"), dst = Path.Combine(data, "EnglishModern");

var books = Directory.GetFiles(src, "*.txt").OrderBy(f => f).ToList();
var verses = books.ToDictionary(f => Path.GetFileNameWithoutExtension(f),
    f => File.ReadAllLines(f, Encoding.UTF8).Select(l => Regex.Match(l, @"^(\d+:\d+) (.*)$")).Where(m => m.Success)
          .Select(m => (Ref: m.Groups[1].Value, Text: m.Groups[2].Value)).ToList());

// The text's own vocabulary, for finding a verb's base form (cometh -> come, sitteth -> sit).
var vocabulary = new HashSet<string>(verses.Values.SelectMany(v => v).SelectMany(v => Regex.Matches(v.Text, @"[a-z]+").Select(m => m.Value)));
var m = new Modernizer(vocabulary);

// Hand edits of verses the rules leave stiff (polish.json, in this folder): applied only where the
// rules still give exactly the text the edit was made from, so a changed rule or source never gets a stale edit.
var polished = File.Exists("polish.json")
    ? System.Text.Json.JsonSerializer.Deserialize<List<PolishEdit>>(File.ReadAllText("polish.json"))!
        .ToDictionary(e => (e.Book, e.Ref))
    : [];
int polishUsed = 0, polishStale = 0;
var stale = new List<string>();

Directory.CreateDirectory(dst);
var samples = new List<string>();
var polish = new List<string>();
int n = 0;
foreach (var (book, list) in verses)
{
    var lines = new List<string>();
    foreach (var (r, t) in list)
    {
        string modern = m.Modernize(t);
        if (polished.TryGetValue((book, r), out var edit))
        {
            if (edit.From == modern) { modern = edit.To; polishUsed++; }
            else { polishStale++; stale.Add($"{book} {r}\n  edit was made from: {edit.From}\n  the rules now give:  {modern}\n  edit:                {edit.To}"); }
        }
        lines.Add($"{r} {modern}");
        if (modern != t && n++ % 97 == 0) samples.Add($"{book} {r}\n  JPS 1917: {t}\n  Modern:   {modern}");
        if (Modernizer.NeedsPolish(modern) is { } why) polish.Add($"{book}\t{r}\t{why}\t{t}\t{modern}");
    }
    File.WriteAllLines(Path.Combine(dst, book + ".txt"), lines, new UTF8Encoding(false));
}

// Report: what changed, and archaic forms that remain.
var report = new StringBuilder();
report.AppendLine("Changes (from -> to, count):");
foreach (var kv in m.Changes.OrderByDescending(kv => kv.Value)) report.AppendLine($"  {kv.Key,-36} {kv.Value,6}");
var left = new Dictionary<string, int>();
foreach (var f in Directory.GetFiles(dst, "*.txt"))
    foreach (Match w in Regex.Matches(File.ReadAllText(f), @"\b(?:[Tt]hou|[Tt]hee|[Tt]hy|[Tt]hine|[Yy]e|[Bb]ehold|[Ll]o|\w+eth|\w+est|\w+edst|\w+dst)\b"))
        if (!Modernizer.NotArchaic(w.Value)) left[w.Value] = left.GetValueOrDefault(w.Value) + 1;
report.AppendLine().AppendLine("Possibly archaic forms left (review):");
foreach (var kv in left.OrderByDescending(kv => kv.Value)) report.AppendLine($"  {kv.Key,-24} {kv.Value,6}");
report.AppendLine().AppendLine("Sample verses:");
foreach (var s in samples) report.AppendLine(s);
File.WriteAllText("changes.txt", report.ToString(), new UTF8Encoding(false));
File.WriteAllLines("needs-polish.txt", polish, new UTF8Encoding(false));
File.WriteAllLines("polish-stale.txt", stale, new UTF8Encoding(false));
Console.WriteLine($"Wrote {verses.Sum(v => v.Value.Count):N0} verses to {dst}; {m.Changes.Values.Sum():N0} words changed " +
                  $"({m.Changes.Count} kinds); {left.Values.Sum()} possibly archaic words left; {polish.Count:N0} verses for a final edit. " +
                  "See changes.txt and needs-polish.txt.");
if (polished.Count > 0)
    Console.WriteLine($"Applied {polishUsed:N0} polished verses from polish.json" +
                      (polishStale > 0 ? $"; {polishStale} skipped because the rules now give different text (redo those edits; see polish-stale.txt)." : "."));
return 0;

/// <summary>An edit of one verse: the rules' output (From) and its polished form (To).</summary>
record PolishEdit(string Book, string Ref, string From, string To);

class Modernizer(HashSet<string> vocabulary)
{
    public readonly Dictionary<string, int> Changes = [];

    // Words always replaced (lower case; case is kept: "Unto" -> "To").
    static readonly Dictionary<string, string> Words = new()
    {
        // Pronouns.
        ["thou"] = "you", ["thee"] = "you", ["ye"] = "you", ["thy"] = "your", ["thyself"] = "yourself",
        // The "thou" verb forms: always second person.
        ["art"] = "are", ["hast"] = "have", ["hadst"] = "had", ["didst"] = "did", ["dost"] = "do", ["doest"] = "do",
        ["wast"] = "were", ["wert"] = "were", ["shalt"] = "shall", ["wilt"] = "will", ["canst"] = "can",
        ["couldest"] = "could", ["wouldest"] = "would", ["shouldest"] = "should", ["mayest"] = "may", ["mightest"] = "might",
        ["saidst"] = "said", ["knewest"] = "knew", ["camest"] = "came", ["wentest"] = "went", ["sawest"] = "saw",
        ["gavest"] = "gave", ["tookest"] = "took", ["madest"] = "made", ["broughtest"] = "brought", ["heardest"] = "heard",
        ["spakest"] = "spoke", ["spokest"] = "spoke", ["borest"] = "bore", ["sentest"] = "sent", ["hadest"] = "had",
        ["becamest"] = "became", ["didest"] = "did",
        // Old verb forms.
        ["saith"] = "says", ["spake"] = "spoke", ["hath"] = "has", ["doth"] = "does", ["doeth"] = "does", ["goeth"] = "goes",
        ["sware"] = "swore", ["brake"] = "broke", ["builded"] = "built", ["digged"] = "dug", ["arose"] = "rose",
        ["beheld"] = "saw", ["begat"] = "fathered", ["begot"] = "fathered", ["begotten"] = "fathered", ["wot"] = "know",
        ["wist"] = "knew", ["shew"] = "show", ["shewed"] = "showed", ["shewn"] = "shown",
        // Verbs with a modern equivalent (also used for their -eth/-est forms, via VerbBases).
        ["dwell"] = "live", ["dwelt"] = "lived", ["dwellings"] = "homes", ["hearken"] = "listen", ["hearkened"] = "listened",
        ["slay"] = "kill", ["slew"] = "killed", ["slain"] = "killed", ["smite"] = "strike", ["smote"] = "struck",
        ["smitten"] = "struck",
        // Other obsolete words.
        ["unto"] = "to", ["upon"] = "on", ["forth"] = "out", ["thence"] = "from there", ["hence"] = "from here",
        ["hither"] = "here", ["thither"] = "there", ["whither"] = "where", ["hitherto"] = "until now",
        ["thereof"] = "of it", ["therein"] = "in it", ["thereon"] = "on it", ["thereto"] = "to it", ["thereunto"] = "to it",
        ["thereby"] = "by it", ["therewith"] = "with it", ["wherein"] = "in which", ["whereof"] = "of which",
        ["whereon"] = "on which", ["whereby"] = "by which", ["wherewith"] = "with which", ["whereupon"] = "after which",
        ["verily"] = "truly", ["peradventure"] = "perhaps", ["howbeit"] = "however", ["insomuch"] = "so much",
        ["aught"] = "anything", ["naught"] = "nothing", ["nought"] = "nothing", ["twain"] = "two", ["betwixt"] = "between", ["amongst"] = "among",
        ["amidst"] = "amid", ["whilst"] = "while", ["ere"] = "before", ["afore"] = "before", ["nay"] = "no", ["yea"] = "indeed",
        ["oft"] = "often", ["privily"] = "secretly", ["straightway"] = "immediately", ["whatsoever"] = "whatever",
        ["whosoever"] = "whoever", ["whomsoever"] = "whoever", ["wheresoever"] = "wherever", ["whoso"] = "whoever",
        ["whithersoever"] = "wherever", ["affrighted"] = "afraid",
        ["exceedingly"] = "greatly", ["exceeding"] = "very", ["wroth"] = "angry", ["midst"] = "middle",
        // Old nouns (after Cohn).
        ["kine"] = "cows", ["brethren"] = "brothers", ["corn"] = "grain", ["lad"] = "boy", ["lads"] = "boys",
        ["raiment"] = "clothes", ["savoury"] = "tasty", ["ass"] = "donkey", ["asses"] = "donkeys", ["damsel"] = "young woman",
        ["damsels"] = "young women", ["kindred"] = "family", ["vale"] = "valley", ["firmament"] = "expanse",
        ["herdmen"] = "herdsmen", ["stead"] = "place", ["victuals"] = "food",
        // American spelling.
        ["favour"] = "favor", ["honour"] = "honor", ["honoured"] = "honored", ["labour"] = "labor", ["neighbour"] = "neighbor",
        ["neighbours"] = "neighbors", ["colour"] = "color", ["colours"] = "colors", ["savour"] = "savor", ["vapour"] = "vapor",
        ["rumour"] = "rumor", ["harbour"] = "harbor", ["valour"] = "valor", ["odour"] = "odor", ["splendour"] = "splendor",
        ["armour"] = "armor", ["fervour"] = "fervor", ["behaviour"] = "behavior", ["saviour"] = "savior",
        ["favoured"] = "favored", ["laboured"] = "labored", ["honourable"] = "honorable", ["favourable"] = "favorable",
        ["vigour"] = "vigor", ["clamour"] = "clamor", ["ardour"] = "ardor", ["parlour"] = "parlor", ["humour"] = "humor",
    };

    /// <summary>Base forms with a modern verb, for -eth/-est forms (dwelleth -> lives, smiteth -> strikes).</summary>
    static readonly Dictionary<string, string> VerbBases = new()
    {
        ["dwell"] = "live", ["hearken"] = "listen", ["slay"] = "kill", ["smite"] = "strike", ["behold"] = "see",
    };

    // Phrases, applied first, on the original text: (pattern, replacement); "$1" keeps the case of the first letter.
    static readonly (Regex Pattern, string To)[] Phrases =
    [
        (new(@"\b([Ff])orasmuch as\b"), "since"),
        // "Wherefore" asks "why" in a question; otherwise it means "therefore".
        (new(@"\b([Ww])herefore\b(?=[^.?!;:]*\?)"), "why"),
        (new(@"\b([Ww])herefore\b"), "therefore"),
        // "brought you forth out of Egypt": forth -> out would double the "out".
        (new(@"\b([Ff])orth out\b"), "out"),
        (new(@"\b([Ff])rom whence\b"), "from where"),
        (new(@"\b([Ff])rom thence\b"), "from there"),
        (new(@"\b([Ff])rom hence\b"), "from here"),
        (new(@"\b([Ww])hence\b"), "from where"),
        (new(@", ([Ii]|[Ww]e) pray (thee|you),"), ", please,"),
        (new(@"\b([Ii]|[Ww]e) pray (thee|you),? ?"), "please "),
        (new(@"\b([Cc])ame to pass\b"), "happened"),
        (new(@"\b([Cc])ome to pass\b"), "happen"),
        (new(@"\b([Cc])ometh to pass\b"), "happens"),
        (new(@"\b([Cc])alled his name\b"), "named him"),
        (new(@"\b([Cc])alled her name\b"), "named her"),
        (new(@"\b([Cc])alled their name\b"), "named them"),
        (new(@"\b([Cc])alled the name thereof\b"), "named it"),
        (new(@"\b([Cc])alled the name of\b"), "named"),
        (new(@"\b([Ii])n the midst of\b"), "in the middle of"),
        (new(@"\b([Dd])id eat\b"), "ate"),
        (new(@"\b([Dd])id drink\b"), "drank"),
        (new(@"\b([Hh])ere am I\b"), "here I am"),
        // "Get thee unto the land" -> "Go to the land"; "get you into" -> "go into".
        (new(@"(?<=(?:^|[,;:.!?'""]\s*|\band ))([Gg])et (?:thee|you) (?=(?:into|to|unto|from)\b)"), "$1o "),
        // Reflexive commands: "Make thee an ark" -> "Make yourself an ark", "Take you a lamb" -> "Take yourselves a lamb".
        // Only at the start of a command, so "I will make thee a great nation" keeps "you".
        (new(@"(?<=(?:^|[,;:.!?'""]\s*|\band ))([Mm]ake|[Tt]ake|[Gg]ird|[Pp]repare|[Cc]hoose|[Pp]rovide|[Bb]uild|[Gg]et) thee\b(?! (?:not|out|up|down|hence|away|gone|into|to|unto|from)\b)"), "$1 yourself"),
        (new(@"(?<=(?:^|[,;:.!?'""]\s*|\band ))([Mm]ake|[Tt]ake|[Gg]ird|[Pp]repare|[Cc]hoose|[Pp]rovide|[Bb]uild|[Gg]et) you\b(?! (?:not|out|up|down|hence|away|gone|into|to|unto|from)\b)"), "$1 yourselves"),
        (new(@"\b([Tt])o-day\b"), "today"),
        (new(@"\b([Tt])o-morrow\b"), "tomorrow"),
        (new(@"\b([Tt])o-night\b"), "tonight"),
        // "she bare a son" (not "bare feet").
        (new(@"\b([Bb])are(?= (a|an|him|her|them|unto|to|sons|daughters|children|his|their|Jacob|Isaac)\b)"), "bore"),
        // behold / lo: dropped inside a sentence, "Look," where it opens one.
        (new(@"\b([Aa])nd,? (behold|lo),? "), "and "),
        (new(@", (behold|lo), "), ", "),
        (new(@"(?<=^|[.?!:;'""] ?)([Bb])ehold,? "), "look, "),
        (new(@"(?<=^|[.?!:;'""] ?)([Ll])o,? "), "look, "),
        (new(@"\b([Bb])ehold\b"), "see"),
        (new(@"\b([Ll])o\b(?=[,!])"), "look"),
    ];

    // Words ending in -eth/-est/-st that are not old verb forms.
    static readonly HashSet<string> NotVerbs = new(StringComparer.OrdinalIgnoreCase)
    {
        "teeth", "beneath", "breath", "death", "seth", "heth", "beth", "japheth", "nazareth", "kenath", "jabneeth", "bosheth",
        "priest", "midst", "first", "cast", "rest", "host", "east", "lest", "beast", "feast", "most", "dust", "west",
        "forest", "harvest", "trust", "must", "fast", "thrust", "uttermost", "just", "best", "least", "against", "honest",
        "nest", "chest", "guest", "breast", "interest", "manifest", "earnest", "almost", "last", "past", "vast", "mast",
        "coast", "roast", "boast", "toast", "post", "cost", "lost", "frost", "ghost", "list", "fist", "mist", "wrist",
        "twist", "exist", "assist", "insist", "resist", "persist", "consist", "desist", "request", "quest", "conquest",
        "contest", "protest", "test", "vest", "zest", "crest", "jest", "pest", "arrest", "invest", "suggest", "digest",
        "detest", "attest", "infest", "molest", "wrest", "lust", "gust", "rust", "crust", "bust", "adjust", "robust",
        "unjust", "distrust", "mistrust", "art", "blast", "steadfast", "breakfast", "outcast", "aghast", "contrast",
        "outmost", "inmost", "foremost", "utmost", "topmost", "hindmost", "nethermost", "innermost", "outermost",
        "uppermost", "lowest", "highest", "modest", "furthest", "farthest", "oldest", "eldest", "youngest", "amethyst",
        "unrest", "behest", "bequest", "tempest", "unto", "amid", "hoarfrost", "southwest", "northwest", "southeast",
        "northeast", "hearth", "wreath", "sheath", "heath", "earth", "girth", "breadth", "eleventh", "sabbath", "goliath",
    };

    public static bool NotArchaic(string w) =>
        NotVerbs.Contains(w) || Regex.IsMatch(w, "^[A-Z]") && !Regex.IsMatch(w, "^(Thou|Thee|Thy|Thine|Ye|Behold|Lo)$")  // names
        || Regex.IsMatch(w, "ieth$")                                                                                    // twentieth
        || Regex.IsMatch(w, "(est)$") && IsSuperlative(w);

    // Superlatives (greatest, poorest) and other adjectives in -est: not verbs.
    static bool IsSuperlative(string w) =>
        Regex.IsMatch(w, "(greatest|fairest|strongest|wisest|choicest|highest|lowest|nearest|smallest|largest|mightiest|" +
                         "fattest|holiest|goodliest|chiefest|meanest|basest|uttermost|innermost|dearest|sorest|furthest|" +
                         "poorest|finest|fewest|hottest|sweetest|richest|lightest|deepest|longest|oldest|youngest|hardest|" +
                         "chiefest|thickest|loftiest|fiercest|surest|purest|ablest|latest|loudest|coolest|sharpest|weakest)$");

    // Constructions the rules leave stiff, for a final light edit: "knew not", "fear not", leftover -est forms.
    static readonly HashSet<string> NotNegation = new(StringComparer.OrdinalIgnoreCase)
    {
        "is", "are", "was", "were", "be", "been", "am", "do", "does", "did", "have", "has", "had", "will", "would", "shall",
        "should", "can", "could", "may", "might", "must", "need", "dare", "and", "but", "or", "nor", "if", "why", "whether",
        "yet", "that", "so", "as", "not", "it", "me", "you", "him", "her", "them", "us", "lest", "though", "until", "also",
        "even", "surely", "certainly", "indeed", "because", "for", "then", "now", "still", "of", "whom", "who", "which",
        "said", "says", "answered", "replied", "this", "these", "those", "there", "here", "too", "is", "almost",
    };

    public static string? NeedsPolish(string s)
    {
        foreach (Match x in Regex.Matches(s, @"\b([a-z]+) not\b"))                       // "knew not", "fear not"
            if (!NotNegation.Contains(x.Groups[1].Value) && x.Groups[1].Value is not ("they" or "he" or "she" or "we" or "in" or "on" or "at" or "by"))
                return $"\"{x.Value}\"";
        foreach (Match x in Regex.Matches(s, @"\b\w+(eth|est|edst)\b"))                  // leftover verb forms
            if (!NotArchaic(x.Value)) return $"\"{x.Value}\"";
        // Emphatic "did" + verb ("G-d did prove Abraham"), not a question ("did he go").
        if (Regex.Match(s, @"\b([Dd]id|[Dd]oes|[Dd]o) (?!not\b|he\b|she\b|it\b|they\b|we\b|you\b|I\b|the\b|this\b|that\b|these\b|those\b|my\b|your\b|his\b|her\b|our\b|their\b|so\b|to\b|according\b|all\b|as\b|in\b|with\b|good\b|evil\b|what\b|them\b|him\b|me\b|us\b|for\b|any\b|after\b|nothing\b|anything\b|thus\b|likewise\b|well\b|justice\b|judgment\b|right\b|wrong\b|wickedly\b|kindness\b|mercy\b|honor\b|homage\b|service\b|battle\b|sacrifice\b|whatever\b|whatsoever\b|great\b|wondrous\b|mighty\b|more\b|abominably\b|corruptly\b|foolishly\b|valiantly\b|exceedingly\b|presumptuously\b)[a-z]{3,}\b") is { Success: true } d)
            return $"\"{d.Value}\"";
        // Reflexive commands ("get you out", "take you a wife") and the old perfect ("is fled", "are come").
        // Only at the start of a command: "I will make you a great nation" is modern English.
        if (Regex.Match(s, @"(?<=(?:^|[,;:.!?'""]\s*))([Gg]et|[Tt]ake|[Mm]ake|[Gg]ird|[Hh]ide|[Ss]hake|[Bb]ow) you\b") is { Success: true } r) return $"\"{r.Value}\"";
        if (Regex.Match(s, @"\b(is|are|was|were) (come|gone|fled|risen|departed|become|returned|entered|arrived|fallen)\b") is { Success: true } p)
            return $"\"{p.Value}\"";
        return null;
    }

    public string Modernize(string s)
    {
        foreach (var (p, to) in Phrases)
            s = p.Replace(s, x => Note(x.Value, Case(x.Value, x.Result(to))));

        // Words are taken one at a time, with the word before and after, for the "thou" verbs, thine/mine and shall.
        var tokens = Regex.Split(s, @"(\b[A-Za-z]+\b)");
        var original = (string[])tokens.Clone();
        string Prev(int i) => i >= 2 && Regex.IsMatch(tokens[i - 1], @"^ +$") ? original[i - 2] : "";
        string Next(int i) => i + 2 < tokens.Length && Regex.IsMatch(tokens[i + 1], @"^ +$") ? original[i + 2] : "";
        bool ClauseStart(int i) => i < 2 || Regex.IsMatch(tokens[i - 1], @"[,;:.!?'""(—]\s*$") ||
                                   Regex.IsMatch(original[i - 2], "^(and|And|then|Then|now|Now|but|But)$") && Regex.IsMatch(tokens[i - 1], @"^ +$");

        for (int i = 1; i < tokens.Length; i += 2)
        {
            string w = original[i], lw = w.ToLowerInvariant();
            string next = Next(i), lnext = next.ToLowerInvariant();

            if (lw is "thine" or "mine")
            {
                // Before a word starting with a vowel or h ("thine eyes", "mine own") it means your/my; standing alone
                // ("the land is thine") it means yours/mine.
                bool attributive = next.Length > 0 && Regex.IsMatch(lnext, "^[aeiouh]") &&
                                   lnext is not ("is" or "are" or "and" or "also" or "or" or "in" or "it" or "as" or "at" or "unto" or "hath" or "art" or "i" or "he" or "her" or "him" or "his" or "how");
                string to = lw == "thine" ? (attributive ? "your" : "yours") : (attributive ? "my" : "mine");
                if (to != lw) tokens[i] = Note(w, Case(w, to));
                continue;
            }
            // "be thou strong", "speak thou unto them": the "thou" of a command is dropped. Not when "thou" is the
            // subject of a verb that follows ("whom thou hast", "since thou art").
            // The same for "ye" ("Comfort ye, comfort ye My people"), but not "know ye not", a question.
            if ((lw == "thou" || lw == "ye" && lnext != "not") && i >= 3 && ClauseStart(i - 2) &&
                Regex.IsMatch(tokens[i - 1], @"^ +$") && IsCommandVerb(original[i - 2]) && !IsThouVerb(lnext))
            {
                Note(original[i - 2] + " " + w, original[i - 2]);
                tokens[i - 1] = tokens[i] = "";
                continue;
            }
            // shall -> will, except when it is addressed to "you" ("you shall not murder"); "shall I?" -> "should I?".
            if (lw == "shall")
            {
                string prev = Prev(i).ToLowerInvariant();
                if (prev is "thou" or "ye" or "you" || lnext is "thou" or "ye" or "you") continue;
                tokens[i] = Note(w, Case(w, lnext is "i" or "we" ? "should" : "will"));
                continue;
            }
            if (Words.TryGetValue(lw, out var word))
            {
                if (lw == "art" && !IsThou(Prev(i)) && !IsThou(next)) continue;   // "the apothecary's art"
                tokens[i] = Note(w, Case(w, word));
                continue;
            }
            if (lw.Length > 4 && lw.EndsWith("eth") && !NotArchaic(w) && !tokens[i - 1].EndsWith('-') && ThirdPerson(lw) is { } third)
            {
                tokens[i] = Note(w, Case(w, third));
                continue;
            }
            // -est verbs: next to "thou", or anywhere when the word is clearly a verb ("and makest", "sittest").
            if (lw.Length > 4 && (lw.EndsWith("est") || lw.EndsWith("edst")) && !NotArchaic(w) && SecondPerson(lw) is { } second &&
                (IsThou(Prev(i)) || IsThou(next) || IsThouBefore(original, i) || IsVerb(second)))
                tokens[i] = Note(w, Case(w, second));
        }
        // Tidy the spaces left by dropped words and a capital lost at the start.
        string result = Regex.Replace(string.Concat(tokens), " {2,}", " ").Replace(" ,", ",");
        result = result.Length > 0 && char.IsLower(result[0]) && char.IsUpper(s[0]) ? char.ToUpperInvariant(result[0]) + result[1..] : result;
        return Syntax(result);
    }

    // ---- Sentence forms: old negatives, emphatic do/did, the old perfect, "get you out". ----

    // Irregular verbs: base, past, past participle.
    static readonly (string Base, string Past, string Participle)[] Irregular =
    [
        ("know", "knew", "known"), ("go", "went", "gone"), ("come", "came", "come"), ("see", "saw", "seen"),
        ("take", "took", "taken"), ("give", "gave", "given"), ("eat", "ate", "eaten"), ("drink", "drank", "drunk"),
        ("speak", "spoke", "spoken"), ("find", "found", "found"), ("hear", "heard", "heard"), ("say", "said", "said"),
        ("get", "got", "gotten"), ("make", "made", "made"), ("bring", "brought", "brought"), ("think", "thought", "thought"),
        ("seek", "sought", "sought"), ("keep", "kept", "kept"), ("leave", "left", "left"), ("tell", "told", "told"),
        ("stand", "stood", "stood"), ("sit", "sat", "sat"), ("rise", "rose", "risen"), ("fall", "fell", "fallen"),
        ("forsake", "forsook", "forsaken"), ("forget", "forgot", "forgotten"), ("hide", "hid", "hidden"),
        ("fight", "fought", "fought"), ("buy", "bought", "bought"), ("sell", "sold", "sold"), ("send", "sent", "sent"),
        ("build", "built", "built"), ("flee", "fled", "fled"), ("feed", "fed", "fed"), ("lead", "led", "led"),
        ("meet", "met", "met"), ("strike", "struck", "struck"), ("break", "broke", "broken"), ("choose", "chose", "chosen"),
        ("run", "ran", "run"), ("begin", "began", "begun"), ("bear", "bore", "borne"), ("swear", "swore", "sworn"),
        ("tear", "tore", "torn"), ("wear", "wore", "worn"), ("throw", "threw", "thrown"), ("grow", "grew", "grown"),
        ("draw", "drew", "drawn"), ("blow", "blew", "blown"), ("fly", "flew", "flown"), ("sleep", "slept", "slept"),
        ("weep", "wept", "wept"), ("hold", "held", "held"), ("sing", "sang", "sung"), ("ride", "rode", "ridden"),
        ("write", "wrote", "written"), ("drive", "drove", "driven"), ("become", "became", "become"),
        ("understand", "understood", "understood"), ("withhold", "withheld", "withheld"), ("bind", "bound", "bound"),
        ("lose", "lost", "lost"), ("deal", "dealt", "dealt"), ("lie", "lay", "lain"), ("lay", "laid", "laid"),
        ("set", "set", "set"), ("put", "put", "put"), ("let", "let", "let"), ("shut", "shut", "shut"), ("cast", "cast", "cast"),
        ("hurt", "hurt", "hurt"), ("cut", "cut", "cut"), ("spread", "spread", "spread"), ("shed", "shed", "shed"),
        ("teach", "taught", "taught"), ("catch", "caught", "caught"), ("spend", "spent", "spent"), ("lend", "lent", "lent"),
        ("bend", "bent", "bent"), ("dig", "dug", "dug"), ("stick", "stuck", "stuck"), ("win", "won", "won"),
        ("shine", "shone", "shone"), ("shake", "shook", "shaken"), ("wake", "woke", "woken"), ("forbid", "forbade", "forbidden"),
        ("forgive", "forgave", "forgiven"), ("hang", "hanged", "hanged"), ("kill", "killed", "killed"), ("feel", "felt", "felt"),
        ("mean", "meant", "meant"), ("dwell", "dwelt", "dwelt"), ("live", "lived", "lived"), ("tread", "trod", "trodden"),
        ("smite", "smote", "smitten"), ("arise", "arose", "arisen"), ("awake", "awoke", "awoken"), ("sink", "sank", "sunk"),
        ("spring", "sprang", "sprung"), ("slay", "slew", "slain"), ("swallow", "swallowed", "swallowed"), ("do", "did", "done"),
    ];
    static readonly Dictionary<string, string> PastToBase = Irregular.Where(v => v.Past != v.Base)
        .GroupBy(v => v.Past).ToDictionary(g => g.Key, g => g.First().Base);
    static readonly Dictionary<string, (string Past, string Participle)> BaseForms = Irregular
        .GroupBy(v => v.Base).ToDictionary(g => g.Key, g => (g.First().Past, g.First().Participle));

    // Words that are never the verb in "X not": helpers, and common non-verbs.
    static readonly HashSet<string> Helpers = new(StringComparer.OrdinalIgnoreCase)
    {
        "is", "are", "was", "were", "be", "been", "am", "do", "does", "did", "have", "has", "had", "will", "would", "shall",
        "should", "can", "could", "may", "might", "must", "need", "dare", "ought", "cannot",
    };
    static readonly HashSet<string> Subjects = new(StringComparer.OrdinalIgnoreCase)
    {
        "i", "we", "you", "they", "he", "she", "it", "who", "that", "which", "one", "these", "those", "this", "all", "people",
        "men", "children", "man", "woman", "king", "priest", "priests", "prophet", "prophets", "eyes", "ears", "heart", "soul",
    };

    /// <summary>A verb's base form: an irregular verb, or a word of the text that the text also shows inflected
    /// (prove/proved, listen/listened, turn/turneth) and is not a pronoun or a small word.</summary>
    bool IsVerb(string b) =>
        b.Length > 1 && !Subjects.Contains(b) && !NotNegation.Contains(b) && !NotCommandVerbs.Contains(b) &&
        (BaseForms.ContainsKey(b) || VerbBases.ContainsValue(b) ||   // the modern verbs the rules bring in: listen, live, kill...
         vocabulary.Contains(b) && (vocabulary.Contains(b + "ed") || vocabulary.Contains(b + "d") || vocabulary.Contains(b + "eth") ||
                                    b.EndsWith('y') && vocabulary.Contains(b[..^1] + "ied") || vocabulary.Contains(b + b[^1] + "ed")));

    /// <summary>"proved" -> "prove", "turned" -> "turn", "stopped" -> "stop", "carried" -> "carry", "knew" -> "know".</summary>
    string? BaseOfPast(string w)
    {
        if (PastToBase.TryGetValue(w, out var b)) return b;
        if (!w.EndsWith("ed") || w.Length < 4) return null;
        string[] candidates = [w[..^1], w[..^2], w.EndsWith("ied") ? w[..^3] + "y" : "", w.Length > 4 && w[^3] == w[^4] ? w[..^3] : ""];
        return candidates.FirstOrDefault(c => c.Length > 1 && IsVerb(c));
    }

    /// <summary>"prove" -> "proved", "turn" -> "turned", "know" -> "knew".</summary>
    string Past(string b) =>
        BaseForms.TryGetValue(b, out var f) ? f.Past
        : vocabulary.Contains(b + "ed") ? b + "ed"
        : vocabulary.Contains(b + "d") ? b + "d"
        : Regex.IsMatch(b, "[^aeiou]y$") ? b[..^1] + "ied"
        : b.EndsWith('e') ? b + "d" : b + "ed";

    /// <summary>"knows" -> "know", "goes" -> "go", "carries" -> "carry".</summary>
    string? BaseOfThird(string w)
    {
        if (!w.EndsWith('s') || w.Length < 3) return null;
        string[] candidates = [w.EndsWith("ies") ? w[..^3] + "y" : "", w.EndsWith("es") ? w[..^2] : "", w[..^1]];
        return candidates.FirstOrDefault(c => c.Length > 1 && IsVerb(c) && Inflect(c) == w);
    }

    string Syntax(string s)
    {
        // "fear not" -> "do not fear"; "he knows not" -> "he does not know"; "knew him not" -> "did not know him".
        s = Regex.Replace(s, @"\b([A-Za-z]+)(?: (him|her|it|them|me|you|us))? not\b", x =>
        {
            string w = x.Groups[1].Value, lw = w.ToLowerInvariant(), obj = x.Groups[2].Value;
            if (Helpers.Contains(lw) || NotNegation.Contains(lw)) return x.Value;
            string before = s[..x.Index].TrimEnd();
            string prev = Regex.Match(before, @"([A-Za-z\-]+)$").Value.ToLowerInvariant();
            // "has commanded not" -> "has not commanded": after have/has/had only the "not" moves.
            if (prev is "have" or "has" or "had" or "having")
                return Note(x.Value, $"not {w}{(obj.Length > 0 ? " " + obj : "")}");
            bool clauseStart = before.Length == 0 || Regex.IsMatch(before, @"[,;:.!?'""(—]$") ||
                               prev is "and" or "but" or "then" or "now" or "therefore" or "so" or "yet";
            bool question = Regex.IsMatch(s[x.Index..], @"^[^.!;:]*\?");
            bool subjectBefore = Subjects.Contains(prev) || prev == "g-d" || prev == "hashem" ||
                                 Regex.IsMatch(before, @"\b[A-Z][a-z]+$") && !clauseStart;
            string? baseForm = null, helper = null;
            // set, put, cut, shed, lay: the same word as present and past; "shed not innocent blood" is a command.
            if (BaseForms.ContainsKey(lw) && (subjectBefore || clauseStart)) { baseForm = lw; helper = "do"; }
            else if (BaseOfPast(lw) is { } pb) { baseForm = pb; helper = "did"; }
            else if (BaseOfThird(lw) is { } tb && subjectBefore && prev is not ("i" or "we" or "you" or "they")) { baseForm = tb; helper = "does"; }
            else if (IsVerb(lw) && (subjectBefore || clauseStart)) { baseForm = lw; helper = "do"; }
            if (baseForm == null) return x.Value;
            baseForm = VerbBases.GetValueOrDefault(baseForm, baseForm);   // hearken -> listen, dwell -> live

            string result;
            bool questionWord = prev is "why" or "how" or "when" or "where" or "wherefore" or "what" or "therefore";
            if ((clauseStart || questionWord) && helper != "do" && obj is "it" or "you" or "he" or "she" or "they")
                // An inverted question: "Came it not...?" -> "Did it not come...?", "went you not" -> "did you not go".
                result = $"{helper} {obj} not {baseForm}";
            else if (clauseStart && helper == "do" && !subjectBefore)
                // A command: "Fear not" -> "Do not fear"; "fear you not" likewise, or "Do you not fear?" in a question.
                result = obj == "you" ? (question ? $"do you not {baseForm}" : $"do not {baseForm}")
                                      : $"do not {baseForm}{(obj.Length > 0 ? " " + obj : "")}";
            else
                result = $"{helper} not {baseForm}{(obj.Length > 0 ? " " + obj : "")}";
            return Note(x.Value, char.IsUpper(w[0]) ? char.ToUpperInvariant(result[0]) + result[1..] : result);
        });

        // Emphatic do/did: "G-d did prove Abraham" -> "G-d proved Abraham"; "I do set" -> "I set". Not questions.
        s = Regex.Replace(s, @"(?<=\b([A-Za-z][\w\-]*) )(did|does|do) ([a-z]+)\b", x =>
        {
            string prev = x.Groups[1].Value.ToLowerInvariant(), aux = x.Groups[2].Value, v = x.Groups[3].Value;
            if (Regex.IsMatch(s[x.Index..], @"^[^.!;:]*\?")) return x.Value;
            if (prev is "why" or "how" or "what" or "when" or "where" or "whom" or "but" or "or" or "not" or "that" or "to" or "so" or "nor" or "neither" or "as")
                return x.Value;
            if (!IsVerb(v) || Helpers.Contains(v) || v is "not" or "so" or "evil" or "good" or "well" or "right" or "wickedly" or "justly") return x.Value;
            // "and did bake": the subject came earlier in the sentence ("she kneaded it, and did bake").
            bool subject = Subjects.Contains(prev) || prev is "g-d" or "hashem" or "and" || char.IsUpper(x.Groups[1].Value[0]);
            if (!subject) return x.Value;
            string to = aux == "did" ? Past(v) : aux == "does" ? Inflect(v) : v;
            return Note(x.Value, to);
        });

        // The old perfect of verbs of motion: "Joab is fled" -> "Joab has fled", "the time is come" -> "the time has come".
        s = Regex.Replace(s, @"\b(is|are|am|was|were|be) (come|gone|fled|risen|departed|become|returned|entered|arrived|fallen|grown|ascended|descended|passed)\b", x =>
        {
            string aux = x.Groups[1].Value switch { "is" => "has", "are" => "have", "am" => "have", "be" => "have", _ => "had" };
            return Note(x.Value, $"{aux} {x.Groups[2].Value}");
        });

        // "Get you out / up / down / away": the "you" of the command is dropped.
        s = Regex.Replace(s, @"\b([Gg]et) you (up|out|down|away|from here|gone)\b", x => Note(x.Value, $"{x.Groups[1].Value} {x.Groups[2].Value}"));
        return s;
    }

    static bool IsThou(string w) => w is "thou" or "Thou";

    // A "thou" verb form ("art", "hast", "knowest"): "thou" before it is its subject.
    static bool IsThouVerb(string w) =>
        w is "art" or "hast" or "hadst" or "didst" or "dost" or "doest" or "wast" or "wert" or "shalt" or "wilt" or "canst"
            or "couldest" or "wouldest" or "shouldest" or "mayest" or "mightest"
        || Regex.IsMatch(w, "(est|edst)$") && !NotArchaic(w);

    static readonly HashSet<string> NotCommandVerbs = new(StringComparer.OrdinalIgnoreCase)
    {
        "and", "but", "for", "that", "when", "if", "as", "so", "yet", "which", "who", "whom", "whose", "what", "where", "now",
        "then", "therefore", "behold", "o", "also", "even", "neither", "nor", "or", "because", "lest", "though", "although",
        "until", "till", "since", "whereas", "while", "whilst", "moreover", "wherewith", "wherein", "whereof", "whereby",
        "except", "unless", "before", "after", "there", "here", "thus", "hence", "thence", "only", "surely", "verily", "again",
        "likewise", "art", "wilt", "shalt", "hast", "dost", "canst", "didst", "wast", "mayest", "how", "why", "whither",
        "whence", "wherefore", "said", "saith", "unto", "upon", "to", "in", "of", "with", "from", "by", "against", "than",
        "all", "not", "never", "ever", "nay", "yea", "lo", "both", "neither", "either", "whether", "whatsoever", "whosoever",
        "ere", "afterward", "furthermore", "it", "peradventure", "scales", "seeing", "them", "thereof", "these",
        "thither", "today", "those", "this", "me", "him", "her", "us", "you", "he", "she", "we", "they", "i",
        // Helper verbs: "shall ye", "have ye", "are ye" ask questions; their "ye" is the subject.
        "shall", "will", "have", "has", "had", "do", "does", "did", "are", "is", "was", "were", "can", "could", "may", "might",
        "should", "would", "must", "need", "dare", "am",
    };

    // A verb in the command form before "thou": a known base form, not a function word or a "thou" verb form.
    bool IsCommandVerb(string w)
    {
        string lw = w.ToLowerInvariant();
        if (NotCommandVerbs.Contains(lw) || lw == "know") return false;   // "know ye not" asks a question
        return vocabulary.Contains(lw) && !Regex.IsMatch(lw, "(est|st|eth|ed)$") && lw.Length > 1;
    }

    // "thou also knowest", "thou not knowest": thou up to three words before, in the same clause.
    static bool IsThouBefore(string[] tokens, int i)
    {
        for (int k = i - 2, n = 0; k >= 1 && n < 3; k -= 2, n++)
        {
            if (!Regex.IsMatch(tokens[k + 1], @"^ +$")) return false;
            if (IsThou(tokens[k])) return true;
        }
        return false;
    }

    /// <summary>cometh -> comes, sitteth -> sits, dieth -> dies, toucheth -> touches, dwelleth -> lives.</summary>
    string? ThirdPerson(string w)
    {
        if (Base(w[..^3]) is { } b) return Inflect(VerbBases.GetValueOrDefault(b, b));
        // A verb the text uses only in this form (consulteth, hovereth, mixeth): its stem, with the e that English
        // needs after c, g, v and u (enhanceth -> enhances, grieveth -> grieves).
        string stem = w[..^3];
        if (stem.Length < 3) return null;
        if (stem[^1] == stem[^2] && stem[^1] is not ('s' or 'l' or 'f' or 'z')) stem = stem[..^1];      // enwrapp -> enwrap
        else if (Regex.IsMatch(stem, "[cgvu]$") || Regex.IsMatch(stem, "[^aeiou][aeiou](s|z|t|m)$")) stem += "e";  // frustrate, blaspheme
        return Inflect(stem);
    }

    /// <summary>knowest -> know, goest -> go, comest -> come, lovedst -> loved.</summary>
    string? SecondPerson(string w)
    {
        if (w.EndsWith("edst")) return vocabulary.Contains(w[..^2]) ? w[..^2] : null;
        return Base(w[..^3]) is { } b ? VerbBases.GetValueOrDefault(b, b) : null;
    }

    /// <summary>The verb's base form, as the text writes it elsewhere: stem, stem + e, or a doubled final consonant undone.</summary>
    string? Base(string stem)
    {
        if (stem.Length < 1) return null;
        // A consonant-vowel-consonant stem usually drops an e: "hop-eth" is "hope", "bit-eth" is "bite".
        bool cvc = Regex.IsMatch(stem, "[^aeiou][aeiou][^aeiouwxy]$");
        string[] candidates = cvc ? [stem + "e", stem] : [stem, stem + "e"];
        foreach (var c in candidates) if (vocabulary.Contains(c)) return c;
        if (stem.Length > 2 && stem[^1] == stem[^2] && vocabulary.Contains(stem[..^1])) return stem[..^1];   // sitt -> sit
        if (stem.EndsWith("i") && vocabulary.Contains(stem[..^1] + "y")) return stem[..^1] + "y";             // "cri" -> cry
        if (stem.EndsWith("i") && vocabulary.Contains(stem + "e")) return stem + "e";                         // "di" -> die
        return null;
    }

    static string Inflect(string b) =>
        Regex.IsMatch(b, "(s|sh|ch|x|z|o)$") ? b + "es"
        : Regex.IsMatch(b, "[^aeiou]y$") ? b[..^1] + "ies"
        : b + "s";

    static string Case(string like, string to) =>
        like.Length > 0 && to.Length > 0 && char.IsUpper(like[0]) ? char.ToUpperInvariant(to[0]) + to[1..] : to;

    string Note(string from, string to)
    {
        string key = $"{from.ToLowerInvariant()} -> {to.ToLowerInvariant()}";
        Changes[key] = Changes.GetValueOrDefault(key) + 1;
        return to;
    }
}
