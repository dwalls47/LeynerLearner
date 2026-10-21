namespace LeynerLearner.Core;

/// <summary>A holiday Torah reading: a key in Hebcal's holiday-readings.json, plus the Chol HaMoed day
/// (used to choose the maftir on Shabbat Chol HaMoed Sukkot).</summary>
public record HolidayReading(string Key, int? CholHaMoedDay = null);

/// <summary>
/// The holiday, fast-day and Rosh Chodesh Torah readings on a date, named with Hebcal's reading keys.
/// Mirrors @hebcal/leyning's getLeyningOnDate / getLeyningKeyForEvent for the Diaspora and Israel.
/// Readings that replace only the maftir of a weekly portion (Shabbat Rosh Chodesh, Shabbat Chanukah,
/// the four special Shabbatot) are not listed here; they are part of that Shabbat's reading.
/// </summary>
public static class HolidayCalendar
{
    public static List<HolidayReading> On(DateTime date, bool israel)
    {
        date = date.Date;
        var hc = ParashaCalendar.Hc;
        int y = hc.GetYear(date), m = hc.GetMonth(date), d = hc.GetDayOfMonth(date);
        bool leap = hc.IsLeapYear(y), shabbat = date.DayOfWeek == DayOfWeek.Saturday;
        int adar = leap ? 7 : 6, nisan = leap ? 8 : 7, sivan = nisan + 2, tammuz = nisan + 3, av = nisan + 4;
        string OnShabbat(string key) => shabbat ? key + " (on Shabbat)" : key;

        var r = new List<HolidayReading>();
        void Fast(string name) { r.Add(new(name)); r.Add(new(name + " (Mincha)")); }
        // A fast that falls on Shabbat moves to Sunday (Ta'anit Esther moves back to Thursday).
        bool FastDay(int month, int day) =>
            (m == month && d == day && !shabbat) || (m == month && d == day + 1 && date.DayOfWeek == DayOfWeek.Sunday);

        if (m == 1)   // Tishrei
        {
            if (d == 1) r.Add(new(OnShabbat("Rosh Hashana I")));
            if (d == 2) r.Add(new("Rosh Hashana II"));
            if (FastDay(1, 3)) Fast("Tzom Gedaliah");
            if (d == 10) { r.Add(new(OnShabbat("Yom Kippur"))); r.Add(new("Yom Kippur (Mincha)")); }
            if (d == 15) r.Add(new(OnShabbat("Sukkot I")));
            if (d == 16 && !israel) r.Add(new("Sukkot II"));
            int firstChm = israel ? 16 : 17;
            if (d >= firstChm && d <= 20)
            {
                int day = d - firstChm + 1;
                r.Add(shabbat ? new("Sukkot Shabbat Chol ha-Moed", day) : new($"Sukkot Chol ha-Moed Day {day}", day));
            }
            if (d == 21)
            {
                r.Add(new("Sukkot Final Day (Hoshana Raba)"));
                if (israel) r.Add(new("Erev Simchat Torah"));
            }
            if (d == 22)
            {
                if (israel) r.Add(new(OnShabbat("Simchat Torah")));
                else { r.Add(new(OnShabbat("Shmini Atzeret"))); r.Add(new("Erev Simchat Torah")); }
            }
            if (d == 23 && !israel) r.Add(new("Simchat Torah"));
        }

        // Chanukah on weekdays (on Shabbat it changes only the maftir and haftarah of the weekly portion).
        int chanukahDay = (date - ParashaCalendar.H(y, 3, 25)).Days + 1;
        bool chanukah = chanukahDay is >= 1 and <= 8;
        if (chanukah && !shabbat)
            r.Add(new((d == 1 || d == 30) && chanukahDay == 7 ? "Chanukah Day 7 (on Rosh Chodesh)" : $"Chanukah Day {chanukahDay}"));

        if (FastDay(4, 10)) Fast("Asara B'Tevet");

        if (m == adar)
        {
            // Ta'anit Esther: 13 Adar, or Thursday the 11th when the 13th is Shabbat.
            bool thirteenthIsShabbat = ParashaCalendar.H(y, adar, 13).DayOfWeek == DayOfWeek.Saturday;
            if ((d == 13 && !thirteenthIsShabbat) || (d == 11 && thirteenthIsShabbat)) Fast("Ta'anit Esther");
            if (d == 14) r.Add(new("Purim"));
            if (d == 15 && israel && !shabbat) r.Add(new("Shushan Purim"));
        }

        if (m == nisan && d >= 15 && d <= 22)
        {
            if (israel)
            {
                if (shabbat) r.Add(new(d is 15 or 21 ? (d == 15 ? "Pesach I (on Shabbat)" : "Pesach VII (on Shabbat)")
                                                     : "Pesach Shabbat Chol ha-Moed"));
                else if (d <= 21)
                    r.Add(new(d switch
                    {
                        15 => "Pesach I", 16 => "Pesach II (CH''M)", 17 => "Pesach III (CH''M)", 18 => "Pesach IV (CH''M)",
                        19 => "Pesach V (CH''M)", 20 => "Pesach VI (CH''M)", _ => "Pesach VII",
                    }));
            }
            else if (d == 15) r.Add(new(OnShabbat("Pesach I")));
            else if (d == 16) r.Add(new("Pesach II"));
            else if (d <= 20)
            {
                int day = d - 16;
                r.Add(new(shabbat ? "Pesach Shabbat Chol ha-Moed"
                    : day == 2 && date.DayOfWeek == DayOfWeek.Sunday ? "Pesach Chol ha-Moed Day 2 on Sunday"
                    : day == 3 && date.DayOfWeek == DayOfWeek.Monday ? "Pesach Chol ha-Moed Day 3 on Monday"
                    : $"Pesach Chol ha-Moed Day {day}", day));
            }
            else if (d == 21) r.Add(new(OnShabbat("Pesach VII")));
            else if (d == 22) r.Add(new(OnShabbat("Pesach VIII")));
        }

        // Yom HaAtzma'ut: 5 Iyar, moved to Thursday if it falls on Friday or Shabbat, and to Tuesday if on Monday.
        if (m == nisan + 1 && y >= 5708)
        {
            var observed = ParashaCalendar.H(y, nisan + 1, 5);
            observed = observed.DayOfWeek switch
            {
                DayOfWeek.Friday => observed.AddDays(-1),
                DayOfWeek.Saturday => observed.AddDays(-2),
                DayOfWeek.Monday when y >= 5764 => observed.AddDays(1),
                _ => observed,
            };
            if (date == observed) r.Add(new("Yom HaAtzma'ut"));
        }

        if (m == sivan)
        {
            if (israel && d == 6) r.Add(new("Shavuot"));
            if (!israel && d == 6) r.Add(new("Shavuot I"));
            if (!israel && d == 7) r.Add(new(OnShabbat("Shavuot II")));
        }

        if (FastDay(tammuz, 17)) Fast("Tzom Tammuz");
        if (FastDay(av, 9)) { r.Add(new("Tish'a B'Av")); r.Add(new("Tish'a B'Av (Mincha)")); }

        // Weekday Rosh Chodesh (Rosh Chodesh Tevet is read as part of Chanukah).
        if ((d == 30 || (d == 1 && m != 1)) && !shabbat && !chanukah)
            r.Add(new("Rosh Chodesh " + MonthName(d == 30 ? date.AddDays(1) : date)));

        return r;
    }

    /// <summary>Hebrew month name as Hebcal spells it in reading keys.</summary>
    static string MonthName(DateTime date)
    {
        var hc = ParashaCalendar.Hc;
        int y = hc.GetYear(date), m = hc.GetMonth(date);
        string[] names = hc.IsLeapYear(y)
            ? ["Tishrei", "Cheshvan", "Kislev", "Tevet", "Sh'vat", "Adar I", "Adar II", "Nisan", "Iyyar", "Sivan", "Tamuz", "Av", "Elul"]
            : ["Tishrei", "Cheshvan", "Kislev", "Tevet", "Sh'vat", "Adar", "Nisan", "Iyyar", "Sivan", "Tamuz", "Av", "Elul"];
        return names[m - 1];
    }
}
