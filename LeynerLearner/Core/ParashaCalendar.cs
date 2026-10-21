using System.Globalization;

namespace LeynerLearner.Core;

/// <summary>What is read on one Shabbat: one or two weekly portions, or a holiday reading.</summary>
public record ShabbatReading(DateTime Date, int[] Portions, string? Holiday, int ReadingYear)
{
    public bool IsHoliday => Holiday != null;
}

/// <summary>
/// Computes the weekly Torah portion for any Shabbat.
///
/// Each reading year (from the Shabbat after Simchat Torah) is split at fixed anchors that hold
/// in every year type, and the free Shabbatot between anchors are filled with the portions of that
/// stretch, combining the customary pairs when there are too few Shabbatot:
///   B'reisheet .. B'chukotai   up to the Shabbat before Shavuot (Bamidbar is read then)
///   Bamidbar .. D'varim        up to the Shabbat before Tisha B'Av has passed
///   Va'etchanan .. Ki Tavo     up to the Shabbat before Rosh Hashanah (Nitzavim is read then)
///   Nitzavim .. Ha'azinu       up to Sukkot
/// Israel differs only when the 8th day of Pesach or 2nd day of Shavuot falls on Shabbat; Israel then
/// reads ahead and catches up by splitting a pair the Diaspora combines.
/// </summary>
public static class ParashaCalendar
{
    /// <summary>The 54 weekly portions, in order, named as in Hebcal (and its triennial.json).</summary>
    public static readonly string[] Portions =
    [
        "Bereshit", "Noach", "Lech-Lecha", "Vayera", "Chayei Sara", "Toldot", "Vayetzei", "Vayishlach",
        "Vayeshev", "Miketz", "Vayigash", "Vayechi",
        "Shemot", "Vaera", "Bo", "Beshalach", "Yitro", "Mishpatim", "Terumah", "Tetzaveh", "Ki Tisa",
        "Vayakhel", "Pekudei",
        "Vayikra", "Tzav", "Shmini", "Tazria", "Metzora", "Achrei Mot", "Kedoshim", "Emor", "Behar", "Bechukotai",
        "Bamidbar", "Nasso", "Beha'alotcha", "Sh'lach", "Korach", "Chukat", "Balak", "Pinchas", "Matot", "Masei",
        "Devarim", "Vaetchanan", "Eikev", "Re'eh", "Shoftim", "Ki Teitzei", "Ki Tavo", "Nitzavim", "Vayeilech",
        "Ha'azinu", "Vezot Haberakhah",
    ];

    /// <summary>First portion of each pair that is sometimes read combined with the next.</summary>
    public static readonly int[] SometimesCombined = [21, 26, 28, 31, 38, 41, 50];

    const int Vayakhel = 21, Tazria = 26, Acharei = 28, Behar = 31, Chukkat = 38, Mattot = 41,
              Vaetchanan = 44, Nitzavim = 50;

    internal static readonly HebrewCalendar Hc = new();

    /// <summary>Display name, e.g. "Vayakhel-Pekudei" for a combined reading.</summary>
    public static string Name(int[] portions) => string.Join("-", portions.Select(p => Portions[p]));

    public static DateTime UpcomingShabbat(DateTime from)
    {
        var d = from.Date;
        return d.AddDays(((int)DayOfWeek.Saturday - (int)d.DayOfWeek + 7) % 7);
    }

    /// <summary>The reading for the given Shabbat (date must be a Saturday).</summary>
    public static ShabbatReading ForShabbat(DateTime shabbat, bool israel)
    {
        shabbat = shabbat.Date;
        int y = Hc.GetYear(shabbat);
        if (shabbat < FirstShabbatAfter(H(y, 1, 22))) y--;
        return ForReadingYear(y, israel).First(r => r.Date == shabbat);
    }

    /// <summary>All Shabbatot from B'reisheet of year <paramref name="y"/> up to B'reisheet of the next year.</summary>
    public static List<ShabbatReading> ForReadingYear(int y, bool israel)
    {
        var result = Diaspora(y);
        if (israel) ApplyIsraelDifferences(y, result);
        return result;
    }

    static List<ShabbatReading> Diaspora(int y)
    {
        int nisan = Hc.IsLeapYear(y) ? 8 : 7, sivan = nisan + 2, av = nisan + 4;
        var holidays = DiasporaHolidays(y);

        DateTime start = FirstShabbatAfter(H(y, 1, 22));       // after Sh'mini Atzeret / Simchat Torah
        DateTime vaetchanan = FirstShabbatAfter(H(y, av, 9));
        DateTime nitzavim = LastShabbatBefore(H(y + 1, 1, 1));
        DateTime end = FirstShabbatAfter(H(y + 1, 1, 22));      // next year's B'reisheet

        // When the 2nd day of Shavuot is Shabbat, the Diaspora combines Chukkat+Balak.
        int[] forced = H(y, sivan, 7).DayOfWeek == DayOfWeek.Saturday ? [Chukkat] : [];

        var list = new List<ShabbatReading>();
        // Remaining pairs, in the order they are combined when only some of them must be.
        Fill(list, y, holidays, start, vaetchanan, 0, Vaetchanan, forced, [Mattot, Tazria, Acharei, Behar, Vayakhel]);
        Fill(list, y, holidays, vaetchanan, nitzavim, Vaetchanan, Nitzavim, [], []);
        Fill(list, y, holidays, nitzavim, end, Nitzavim, Portions.Length - 1, [], [Nitzavim]);
        return list;
    }

    /// <summary>Assigns portions [first, last) to the non-holiday Shabbatot in [from, to).</summary>
    static void Fill(List<ShabbatReading> list, int y, Dictionary<DateTime, string> holidays,
                     DateTime from, DateTime to, int first, int last, int[] forced, int[] pairPriority)
    {
        var shabbatot = new List<DateTime>();
        for (var d = from; d < to; d = d.AddDays(7)) shabbatot.Add(d);
        int free = shabbatot.Count(d => !holidays.ContainsKey(d));
        int doubles = (last - first) - free - forced.Length;
        if (doubles < 0 || doubles > pairPriority.Length)
            throw new InvalidOperationException($"Cannot schedule {Portions[first]}..{Portions[last - 1]} in year {y}: {free} Shabbatot");
        var combine = forced.Concat(pairPriority.Take(doubles)).ToHashSet();

        int p = first;
        foreach (var d in shabbatot)
        {
            if (holidays.TryGetValue(d, out var h)) { list.Add(new ShabbatReading(d, [], h, y)); continue; }
            int[] portions = combine.Contains(p) ? [p, p + 1] : [p];
            list.Add(new ShabbatReading(d, portions, null, y));
            p += portions.Length;
        }
    }

    static Dictionary<DateTime, string> DiasporaHolidays(int y)
    {
        int nisan = Hc.IsLeapYear(y) ? 8 : 7, sivan = nisan + 2;
        var h = new Dictionary<DateTime, string>();
        for (int d = 15; d <= 22; d++) h[H(y, nisan, d)] = "Pesach";
        h[H(y, sivan, 6)] = "Shavuot";
        h[H(y, sivan, 7)] = "Shavuot";
        h[H(y + 1, 1, 1)] = "Rosh Hashanah";
        h[H(y + 1, 1, 2)] = "Rosh Hashanah";
        h[H(y + 1, 1, 10)] = "Yom Kippur";
        for (int d = 15; d <= 21; d++) h[H(y + 1, 1, d)] = "Sukkot";
        h[H(y + 1, 1, 22)] = "Sh'mini Atzeret";
        h[H(y + 1, 1, 23)] = "Simchat Torah";
        return h;
    }

    static void ApplyIsraelDifferences(int y, List<ShabbatReading> list)
    {
        int nisan = Hc.IsLeapYear(y) ? 8 : 7, sivan = nisan + 2;
        int Idx(DateTime d) => list.FindIndex(r => r.Date == d);
        bool Combined(int portion) => list.Any(r => r.Portions.Length == 2 && r.Portions[0] == portion);

        // Diaspora-only holiday days that can fall on Shabbat: 8th day of Pesach, 2nd day of Shavuot.
        // (23 Tishrei can never be Shabbat.)
        int diverge = -1, target = -1;
        if (Idx(H(y, nisan, 22)) is var p8 and >= 0)
        {
            diverge = p8;
            // Non-leap years: Israel splits B'har/B'chukotai; leap years: the next pair after Pesach (Mattot/Masei).
            target = Combined(Behar) ? Behar : list.Skip(p8).First(r => r.Portions.Length == 2).Portions[0];
        }
        else if (Idx(H(y, sivan, 7)) is var s2 and >= 0)
        {
            diverge = s2;
            target = list.Skip(s2).First(r => r.Portions.Length == 2).Portions[0];
        }
        if (diverge < 0) return;

        // Diaspora readings from the divergence up to and including the target pair, with that pair split.
        int t = list.FindIndex(r => r.Portions.Length == 2 && r.Portions[0] == target);
        var queue = new Queue<int[]>();
        for (int i = diverge + 1; i <= t; i++)
        {
            if (list[i].IsHoliday) continue;
            if (i == t) { queue.Enqueue([target]); queue.Enqueue([target + 1]); }
            else queue.Enqueue(list[i].Portions);
        }
        for (int i = diverge; i <= t; i++)
        {
            if (i != diverge && list[i].IsHoliday) continue;
            list[i] = list[i] with { Portions = queue.Dequeue(), Holiday = null };
        }
    }

    internal static DateTime H(int year, int month, int day) => Hc.ToDateTime(year, month, day, 0, 0, 0, 0);

    internal static DateTime FirstShabbatAfter(DateTime d) => d.AddDays(((int)DayOfWeek.Saturday - (int)d.DayOfWeek + 6) % 7 + 1);

    internal static DateTime LastShabbatBefore(DateTime d) => FirstShabbatAfter(d.AddDays(-8));

    public static string HebrewDate(DateTime d)
    {
        int y = Hc.GetYear(d), m = Hc.GetMonth(d), day = Hc.GetDayOfMonth(d);
        string[] names = Hc.IsLeapYear(y)
            ? ["Tishrei", "Cheshvan", "Kislev", "Tevet", "Sh'vat", "Adar I", "Adar II", "Nisan", "Iyar", "Sivan", "Tammuz", "Av", "Elul"]
            : ["Tishrei", "Cheshvan", "Kislev", "Tevet", "Sh'vat", "Adar", "Nisan", "Iyar", "Sivan", "Tammuz", "Av", "Elul"];
        return $"{day} {names[m - 1]} {y}";
    }
}
