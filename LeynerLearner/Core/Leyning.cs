using System.Text.Json;

namespace LeynerLearner.Core;

public enum HaftarahCustom { None, Ashkenazi, Sephardi, Chabad, Triennial }

/// <summary>A day's Torah reading: a weekly portion on Shabbat, or a holiday/fast/Rosh Chodesh reading.</summary>
public record Occasion(DateTime Date, int[]? Portions, HolidayReading? Holiday)
{
    public string Name => Portions != null ? ParashaCalendar.Name(Portions) : Holiday!.Key;

    /// <summary>A Shabbat or festival (yom tov, chol hamoed), as opposed to weekday Rosh Chodesh,
    /// fast days, Chanukah, Purim and Yom HaAtzma'ut.</summary>
    public bool IsShabbatOrFestival => Portions != null || !(
        Name.StartsWith("Rosh Chodesh") || Name.StartsWith("Chanukah") || Name.Contains("Purim") ||
        Name.StartsWith("Tzom") || Name.StartsWith("Asara") || Name.StartsWith("Ta'anit") ||
        Name.StartsWith("Tish'a B'Av") || Name.StartsWith("Yom HaAtzma'ut"));
    public override string ToString() => $"{Date:ddd MMM d, yyyy} — {Name}";
}

/// <summary>A complete reading ready to display: Torah aliyot and haftarah parts.</summary>
/// <param name="HaftarahLabel">Where the haftarah comes from when it isn't simply the standard one
/// (e.g. "Triennial", "Sephardi", "Shabbat Rosh Chodesh"); null for the standard haftarah.</param>
/// <param name="HaftarahNote">Theme note that comes with a triennial haftarah.</param>
public record Leyning(string Title, string? Special, List<Aliyah> Torah, List<Aliyah> Haftarah,
                      string? HaftarahLabel, string? HaftarahNote);

/// <summary>
/// Holiday readings and haftarot from Hebcal's @hebcal/leyning and @hebcal/triennial data
/// (BSD-2-Clause, see Data\Hebcal\README.md), with Hebcal's rules for special Shabbatot.
/// </summary>
public class LeyningData
{
    record Range(string Book, VerseRef Start, VerseRef End, string? Note);

    class Festival
    {
        public Dictionary<string, Range> FullKriyah = [];
        public List<Range>? Haft, Seph, Chabad;
        public bool? Israel;   // null: both; true/false: only Israel / only the Diaspora
    }

    class ParshaHaftarot { public List<Range>? Haft, Seph, Chabad; }

    readonly Dictionary<string, Festival> festivals = [];
    readonly Dictionary<string, ParshaHaftarot> parshiyot = [];
    readonly Dictionary<string, Dictionary<string, List<Range>>> triennialHaft = [];
    readonly Dictionary<string, List<Range>> triennialHolidayHaft = [];
    readonly Triennial triennial;

    public LeyningData(string hebcalDir)
    {
        triennial = Triennial.Load(Path.Combine(hebcalDir, "triennial.json"));

        using (var doc = Parse(hebcalDir, "holiday-readings.json"))
        {
            var aliases = new Dictionary<string, (string Key, bool? Israel)>();
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                bool? il = p.Value.TryGetProperty("il", out var ilv) ? ilv.GetBoolean() : null;
                if (p.Value.TryGetProperty("alias", out _)) { aliases[p.Name] = (p.Value.GetProperty("key").GetString()!, il); continue; }
                var f = new Festival { Israel = il };
                if (p.Value.TryGetProperty("fullkriyah", out var fk))
                    foreach (var a in fk.EnumerateObject()) f.FullKriyah[a.Name] = ParseRanges(a.Value)[0];
                f.Haft = Optional(p.Value, "haft");
                f.Seph = Optional(p.Value, "seph");
                f.Chabad = p.Value.TryGetProperty("chabad", out var ch) && ch.ValueKind == JsonValueKind.Object && ch.TryGetProperty("sameas", out _) ? f.Haft : Optional(p.Value, "chabad");
                festivals[p.Name] = f;
            }
            foreach (var (name, (key, il)) in aliases)
            {
                var target = festivals[key];
                festivals[name] = new Festival
                {
                    FullKriyah = target.FullKriyah, Haft = target.Haft, Seph = target.Seph, Chabad = target.Chabad,
                    Israel = il ?? target.Israel,
                };
            }
        }

        using (var doc = Parse(hebcalDir, "aliyot.json"))
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                var h = new ParshaHaftarot { Haft = Optional(p.Value, "haft"), Seph = Optional(p.Value, "seph") };
                h.Chabad = p.Value.TryGetProperty("chabad", out var ch) && ch.ValueKind == JsonValueKind.Object && ch.TryGetProperty("sameas", out _) ? h.Haft : Optional(p.Value, "chabad");
                parshiyot[p.Name] = h;
            }

        using (var doc = Parse(hebcalDir, "triennial-haft.json"))
            foreach (var p in doc.RootElement.EnumerateObject())
                triennialHaft[p.Name] = p.Value.EnumerateObject().Where(y => y.Value.ValueKind != JsonValueKind.String)
                    .ToDictionary(y => y.Name, y => ParseRanges(y.Value));

        using (var doc = Parse(hebcalDir, "tri-haft-holidays.json"))
            foreach (var p in doc.RootElement.EnumerateObject())
                triennialHolidayHaft[p.Name] = ParseRanges(p.Value);
    }

    // ---- Occasions ----------------------------------------------------------------------------

    /// <summary>All days with a Torah reading (weekly Shabbat portions and holiday readings) in a date range.</summary>
    public List<Occasion> Occasions(DateTime from, DateTime to, bool israel)
    {
        var list = new List<Occasion>();
        for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
        {
            if (d.DayOfWeek == DayOfWeek.Saturday && ParashaCalendar.ForShabbat(d, israel) is { IsHoliday: false } s)
                list.Add(new Occasion(d, s.Portions, null));
            foreach (var h in HolidayCalendar.On(d, israel))
                if (HasTorahReading(h.Key, israel)) list.Add(new Occasion(d, null, h));
        }
        return list;
    }

    bool HasTorahReading(string key, bool israel) =>
        festivals.TryGetValue(key, out var f) && (f.Israel == null || f.Israel == israel) && f.FullKriyah.ContainsKey("1");

    // ---- Weekly portion on Shabbat ---------------------------------------------------------------

    /// <summary>
    /// The triennial reading of a weekly portion, with the haftarah for the chosen custom. When the
    /// Shabbat date is given, special readings for that day (Rosh Chodesh, Chanukah, Shekalim, ...) apply.
    /// </summary>
    public Leyning ForShabbat(int[] portions, int readingYear, bool israel, DateTime? shabbat, HaftarahCustom custom)
    {
        var t = triennial.Get(portions, readingYear, israel);
        var aliyot = t.Aliyot;
        string name = t.Parasha;
        // Like Hebcal, number the cycle year by the date's Hebrew year. This differs from the reading
        // year only for Vayeilech/Ha'azinu in Tishrei, whose aliyot are the same in every year.
        int cycleYear = shabbat is DateTime sd ? Triennial.CycleYear(ParashaCalendar.Hc.GetYear(sd)) : t.CycleYear;
        var specials = new List<string>();
        Festival? haftSource = null;
        string? haftSourceKey = null;

        void Apply(string key)
        {
            if (!festivals.TryGetValue(key, out var f) || (f.Israel != null && f.Israel != israel)) return;
            bool used = false;
            if (f.Haft != null && haftSource == null) { haftSource = f; haftSourceKey = key; used = true; }
            if (f.FullKriyah.TryGetValue("7", out var seventh))
            {
                // Merge aliyot 6 and 7, then read the special 7th (e.g. Rosh Chodesh).
                int i6 = aliyot.FindIndex(a => a.Label == Aliyah.LabelFor("6")), i7 = aliyot.FindIndex(a => a.Label == Aliyah.LabelFor("7"));
                aliyot[i6] = aliyot[i6] with { End = aliyot[i7].End };
                aliyot[i7] = ToAliyah("7", seventh, key);
                used = true;
            }
            if (f.FullKriyah.TryGetValue("M", out var maftir))
            {
                aliyot.RemoveAll(a => a.Label == Aliyah.LabelFor("M"));
                aliyot.Add(ToAliyah("M", maftir, key));
                used = true;
            }
            if (used) specials.Add(key);
        }

        if (shabbat is DateTime d)
        {
            foreach (var key in SpecialShabbatKeys(d, name)) Apply(key);
            if (haftSource == null && FallbackSpecialKey(d, name) is string fallback) Apply(fallback);
        }

        List<Range>? haft;
        string? label, note = null;
        if (haftSource != null)
        {
            // A special Shabbat's haftarah replaces the portion's (including the triennial alternative).
            haft = Choose(haftSource.Haft, haftSource.Seph, haftSource.Chabad, custom);
            label = haftSourceKey + VariantLabel(custom, haftSource.Seph, haftSource.Chabad, prefix: ", ");
        }
        else
        {
            var p = parshiyot[HaftarahKey(portions)];
            (haft, label, note) = custom == HaftarahCustom.Triennial
                ? TriennialHaftarah(portions, cycleYear) is { } tri
                    ? (tri, "Triennial", tri[0].Note)
                    : (p.Haft, "standard — no separate triennial haftarah", (string?)null)
                : (Choose(p.Haft, p.Seph, p.Chabad, custom), VariantLabel(custom, p.Seph, p.Chabad), (string?)null);
        }
        return new Leyning($"{name} — Triennial Year {cycleYear}",
            specials.Count == 0 ? null : string.Join(", ", specials), aliyot, Haftarah(haft, custom), label, note);
    }

    /// <summary>"Sephardi"/"Chabad" when that custom has its own haftarah here; otherwise it is the Ashkenazi one.</summary>
    static string? VariantLabel(HaftarahCustom custom, List<Range>? seph, List<Range>? chabad, string prefix = "") =>
        custom == HaftarahCustom.Sephardi && seph != null ? prefix + "Sephardi"
        : custom == HaftarahCustom.Chabad && chabad != null ? prefix + "Chabad"
        : prefix.Length > 0 ? "" : null;

    /// <summary>Special Shabbatot that fall on this date, as Hebcal's holiday events would name them.</summary>
    static IEnumerable<string> SpecialShabbatKeys(DateTime d, string parasha)
    {
        var hc = ParashaCalendar.Hc;
        int y = hc.GetYear(d), m = hc.GetMonth(d), day = hc.GetDayOfMonth(d);
        bool leap = hc.IsLeapYear(y), rc = day == 1 || day == 30;
        int adar = leap ? 7 : 6, nisan = leap ? 8 : 7;

        int chanukahDay = (d - ParashaCalendar.H(y, 3, 25)).Days + 1;
        if (chanukahDay is >= 1 and <= 8)
            yield return rc ? "Shabbat Rosh Chodesh Chanukah" : $"Chanukah Day {chanukahDay} (on Shabbat)";

        var hachodesh = OnOrBefore(ParashaCalendar.H(y, nisan, 1));
        if (d == OnOrBefore(ParashaCalendar.H(y, adar, 1))) yield return rc ? "Shabbat Shekalim (on Rosh Chodesh)" : "Shabbat Shekalim";
        if (d == ParashaCalendar.LastShabbatBefore(ParashaCalendar.H(y, adar, 14))) yield return "Shabbat Zachor";
        if (d == hachodesh.AddDays(-7)) yield return "Shabbat Parah";
        if (d == hachodesh) yield return rc ? "Shabbat HaChodesh (on Rosh Chodesh)" : "Shabbat HaChodesh";
        if (d == OnOrBefore(ParashaCalendar.H(y, nisan, 14))) yield return "Shabbat HaGadol";
        if (m == 1 && day is >= 3 and <= 9) yield return $"Shabbat Shuva (with {parasha})";
    }

    /// <summary>Hebcal's rules for a special haftarah when no special Shabbat supplied one.</summary>
    static string? FallbackSpecialKey(DateTime d, string parasha)
    {
        var hc = ParashaCalendar.Hc;
        int y = hc.GetYear(d), m = hc.GetMonth(d), day = hc.GetDayOfMonth(d);
        int av = (hc.IsLeapYear(y) ? 8 : 7) + 4;
        if (parasha == "Pinchas" && day > 17) return "Pinchas occurring after 17 Tammuz";
        if (day is 30 or 1) return parasha is "Masei" or "Matot-Masei" ? $"{parasha} on Shabbat Rosh Chodesh" : "Shabbat Rosh Chodesh";
        if (parasha == "Ki Teitzei" && day == 14) return "Ki Teitzei with 3rd Haftarah of Consolation";
        if (parasha == "Kedoshim" && day is 26 or 28 or 6) return "Kedoshim following Special Shabbat";
        if (m != av && hc.GetDayOfMonth(d.AddDays(1)) is 30 or 1) return "Shabbat Machar Chodesh";
        return null;
    }

    /// <summary>Combined portions read the second portion's haftarah, except Nitzavim-Vayeilech and Achrei Mot-Kedoshim.</summary>
    static string HaftarahKey(int[] portions)
    {
        string first = ParashaCalendar.Portions[portions[0]];
        if (portions.Length == 1 || first == "Nitzavim") return first;
        return first == "Achrei Mot" ? ParashaCalendar.Name(portions) : ParashaCalendar.Portions[portions[1]];
    }

    /// <summary>
    /// Alternative triennial haftarah. For combined portions, year 1 reads the first portion's and
    /// years 2-3 the second portion's (Hebcal's rule).
    /// </summary>
    List<Range>? TriennialHaftarah(int[] portions, int cycleYear)
    {
        int idx = portions.Length == 1 || cycleYear == 1 ? 0 : 1;
        return triennialHaft.TryGetValue(ParashaCalendar.Portions[portions[idx]], out var years)
               && years.TryGetValue(cycleYear.ToString(), out var h) ? h : null;
    }

    // ---- Holidays ---------------------------------------------------------------------------------

    public Leyning ForHoliday(HolidayReading h, DateTime date, HaftarahCustom custom)
    {
        var f = festivals[h.Key];
        var aliyot = new List<Aliyah>();
        foreach (var n in new[] { "1", "2", "3", "4", "5", "6", "7", "8", "M" })
        {
            string key = n == "M" && h.Key == "Sukkot Shabbat Chol ha-Moed" ? $"M-day{h.CholHaMoedDay}" : n;
            if (f.FullKriyah.TryGetValue(key, out var r)) aliyot.Add(ToAliyah(n, r, null));
        }

        List<Range>? haft;
        string? label;
        if (custom == HaftarahCustom.Triennial)
        {
            int year = Triennial.CycleYear(ParashaCalendar.Hc.GetYear(date));
            var tri = h.Key == "Tish'a B'Av"
                ? (triennialHaft.TryGetValue(h.Key, out var y) && y.TryGetValue(year.ToString(), out var t) ? t : null)
                : triennialHolidayHaft.GetValueOrDefault(h.Key);
            (haft, label) = tri != null ? (tri, "Triennial") : (f.Haft, "standard — no separate triennial haftarah");
        }
        else (haft, label) = (Choose(f.Haft, f.Seph, f.Chabad, custom), VariantLabel(custom, f.Seph, f.Chabad));
        return new Leyning(h.Key, null, aliyot, Haftarah(haft, custom), label, haft?.FirstOrDefault()?.Note);
    }

    // ---- helpers ------------------------------------------------------------------------------------

    static List<Range>? Choose(List<Range>? haft, List<Range>? seph, List<Range>? chabad, HaftarahCustom custom) => custom switch
    {
        HaftarahCustom.None => null,
        HaftarahCustom.Sephardi => seph ?? haft,
        HaftarahCustom.Chabad => chabad ?? haft,
        _ => haft,
    };

    static List<Aliyah> Haftarah(List<Range>? parts, HaftarahCustom custom)
    {
        if (parts == null || custom == HaftarahCustom.None) return [];
        return parts.Select((r, i) => new Aliyah(parts.Count == 1 ? "Haftarah" : $"Haftarah, part {i + 1}",
                                                 r.Book, r.Start, r.End)).ToList();
    }

    static Aliyah ToAliyah(string n, Range r, string? note) => new(Aliyah.LabelFor(n), r.Book, r.Start, r.End, note);

    static DateTime OnOrBefore(DateTime d) => ParashaCalendar.LastShabbatBefore(d.AddDays(1));

    static JsonDocument Parse(string dir, string file) => JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, file)));

    static List<Range>? Optional(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) ? ParseRanges(v) : null;

    /// <summary>A range object {k, b, e, note?} or an array of them; k is a Torah book number or a book name.</summary>
    static List<Range> ParseRanges(JsonElement e)
    {
        if (e.ValueKind == JsonValueKind.Array) return e.EnumerateArray().SelectMany(ParseRanges).ToList();
        var k = e.GetProperty("k");
        string book = Books.FromHebcal(k.ValueKind == JsonValueKind.Number ? k.GetInt32().ToString() : k.GetString()!);
        string? note = e.TryGetProperty("note", out var n) ? n.GetString() : null;
        return [new Range(book, VerseRef.Parse(e.GetProperty("b").GetString()!), VerseRef.Parse(e.GetProperty("e").GetString()!), note)];
    }
}
