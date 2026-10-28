using System.Diagnostics;
using LeynerLearner.Core;
using LeynerLearner.Rendering;

namespace LeynerLearner
{
    public partial class LeynerLearner : Form
    {
        /// <summary>A parashah (or combined pair) in the drop-down.</summary>
        sealed record ParashaItem(int[] Portions)
        {
            public override string ToString() => ParashaCalendar.Name(Portions);
        }

        // How far around the chosen date the Reading list extends.
        static readonly TimeSpan ListBefore = TimeSpan.FromDays(60), ListAfter = TimeSpan.FromDays(400);

        // The bundled Data folder, or an updated data pack in %LOCALAPPDATA% (see DataStore).
        DataStore.Choice data = null!;
        string dataDir => data.Dir;
        List<string> loadedFonts = [];

        /// <summary>Fonts in the data folder (path within it, family name). Hebrew Sheva Na is required; the others
        /// are loaded and listed only if their folder is there.</summary>
        static readonly (string File, string Family)[] BundledFonts =
        [
            (@"Fonts\HebrewShevaNa.ttf", ShevaNa.FontFamily),
            (@"FrankRuhlLibre\FrankRuhlLibre-Regular.ttf", "Frank Ruhl Libre"),
        ];

        IEnumerable<(string File, string Family)> AvailableFonts =>
            BundledFonts.Where(f => f.Family == ShevaNa.FontFamily || File.Exists(Path.Combine(dataDir, f.File)));
        ScriptureText text = null!;
        LeyningData leyning = null!;
        ShevaNa shevaNa = null!;
        PhoneticMam phonetic = null!;

        DateTime listFrom, listTo;    // range covered by cboOccasion
        ReadingDocument? current;     // what is displayed (and saved/printed)
        bool updating;

        public LeynerLearner()
        {
            InitializeComponent();
            Icon = AppIcon.Value;
        }

        Occasion? Selected => cboOccasion.SelectedItem as Occasion;
        bool Israel => chkIsrael.Checked;
        HaftarahCustom Custom => (HaftarahCustom)cboHaftarah.SelectedIndex;
        RenderOptions Options => new((cboFont.SelectedItem as FontItem)?.Name ?? "Times New Roman", (float)numSize.Value,
                                     (EnglishVersion)Math.Max(0, cboEnglish.SelectedIndex),
                                     (TransliterationStyle)Math.Max(0, cboTranslit.SelectedIndex));

        void LeynerLearner_Load(object? sender, EventArgs e)
        {
            LoadData();
            if (data.InstalledProblem != null && !data.Outdated)
                BeginInvoke(() => MessageBox.Show(this, $"The updated data in {DataStore.InstalledDir} is not used: {data.InstalledProblem}.\n\n" +
                                                        "The app is using its built-in data. Use \"Update data…\" to download the data again.",
                                                  "Updated data not used", MessageBoxButtons.OK, MessageBoxIcon.Warning));

            updating = true;
            // Weekly portions in Torah order, each combined pair listed right after its first half.
            for (int p = 0; p < ParashaCalendar.Portions.Length - 1; p++)   // V'Zot HaBerachah is read on Simchat Torah
            {
                cboParasha.Items.Add(new ParashaItem([p]));
                if (ParashaCalendar.SometimesCombined.Contains(p)) cboParasha.Items.Add(new ParashaItem([p, p + 1]));
            }
            cboHaftarah.Items.AddRange(Enum.GetNames<HaftarahCustom>());
            cboHaftarah.SelectedIndex = (int)HaftarahCustom.Ashkenazi;

            // Only fonts that can show pointed Hebrew (letters and vowels); those without trope marks are labelled.
            var names = FontFamily.Families.Select(f => f.Name).Where(n => !n.StartsWith('@'))
                                  .Concat(AvailableFonts.Select(f => f.Family)).Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var name in names.Order(StringComparer.OrdinalIgnoreCase))
                if (HebrewFonts.Check(name) is { Hebrew: true } coverage)
                    cboFont.Items.Add(new FontItem(name, coverage.Trope));
            // The large sheva is drawn only by the bundled font, so it is the default font too.
            SelectFont(ShevaNa.FontFamily);
            cboShevaNa.SelectedIndex = (int)ShevaNaStyle.LargeSheva;
            cboTranslit.SelectedIndex = (int)TransliterationStyle.Off;
            cboEnglish.SelectedIndex = (int)EnglishVersion.Off;
            updating = false;

            GoToDate(DateTime.Today, shabbatOrFestival: true);
        }

        /// <summary>Chooses the data folder and opens its text, schedules and font. Book files are read when first needed.</summary>
        void LoadData()
        {
            data = DataStore.Choose();
            text = new ScriptureText(dataDir);
            leyning = new LeyningData(Path.Combine(dataDir, "Hebcal"));
            shevaNa = ShevaNa.Load(Path.Combine(dataDir, "MAM", "sheva-na.json"));
            phonetic = new PhoneticMam(Path.Combine(dataDir, "PhoneticMAM"), text);

            // The bundled fonts (Hebrew Sheva Na has the large sounded-sheva glyph) are loaded for this process only,
            // from copies, so the data folder can be replaced while the app runs.
            var fonts = DataStore.FontCopies(AvailableFonts.Select(f => Path.Combine(dataDir, f.File)));
            foreach (var old in loadedFonts.Except(fonts)) RemoveFontResourceEx(old, FR_PRIVATE, IntPtr.Zero);
            foreach (var font in fonts.Except(loadedFonts))
            {
                AddFontResourceEx(font, FR_PRIVATE, IntPtr.Zero);
                PdfRenderer.RegisterFont(font);
            }
            loadedFonts = fonts;
        }

        /// <summary>Runs a change to the installed data pack, then reloads the data and shows the same date again.</summary>
        void ReplaceData(Action change)
        {
            var date = Selected?.Date ?? DateTime.Today;
            try
            {
                UseWaitCursor = true;
                change();
            }
            finally
            {
                LoadData();
                FillOccasions(date);
                GoToDate(date);
                UseWaitCursor = false;
            }
        }

        void btnData_Click(object? sender, EventArgs e)
        {
            using var dlg = new DataUpdateForm(() => data, ReplaceData);
            dlg.ShowDialog(this);
        }

        /// <summary>
        /// Select the first reading on or after a date (optionally the first Shabbat or festival),
        /// rebuilding the Reading list if needed.
        /// </summary>
        void GoToDate(DateTime date, bool shabbatOrFestival = false)
        {
            date = date.Date;
            if (cboOccasion.Items.Count == 0 || date < listFrom || date > listTo - ListBefore)
                FillOccasions(date);
            var target = cboOccasion.Items.Cast<Occasion>().FirstOrDefault(o => o.Date >= date && (!shabbatOrFestival || o.IsShabbatOrFestival))
                         ?? cboOccasion.Items.Cast<Occasion>().Last();
            if (ReferenceEquals(cboOccasion.SelectedItem, target)) ShowOccasion();
            else cboOccasion.SelectedItem = target;     // raises SelectedIndexChanged -> ShowOccasion
        }

        void FillOccasions(DateTime around)
        {
            listFrom = around - ListBefore;
            listTo = around + ListAfter;
            if (listFrom < dtpShabbat.MinDate) listFrom = dtpShabbat.MinDate;
            updating = true;
            cboOccasion.BeginUpdate();
            cboOccasion.Items.Clear();
            cboOccasion.Items.AddRange(leyning.Occasions(listFrom, listTo, Israel).ToArray<object>());
            cboOccasion.EndUpdate();
            updating = false;
        }

        /// <summary>Sync the date and parashah controls with the selected reading, then render it.</summary>
        void ShowOccasion()
        {
            if (Selected is not Occasion o) return;
            updating = true;
            dtpShabbat.Value = o.Date;
            bool weekly = o.Portions != null;
            cboParasha.Enabled = cboYear.Enabled = weekly;
            if (weekly)
            {
                cboParasha.SelectedItem = cboParasha.Items.Cast<ParashaItem>().First(i => i.Portions.SequenceEqual(o.Portions!));
                cboYear.SelectedIndex = Triennial.CycleYear(ParashaCalendar.Hc.GetYear(o.Date)) - 1;
            }
            else
            {
                cboParasha.SelectedIndex = -1;
                cboYear.SelectedIndex = -1;
            }
            updating = false;
            RenderReading();
        }

        void RenderReading()
        {
            if (updating || Selected is not Occasion o || cboHaftarah.SelectedIndex < 0) return;

            Leyning l;
            DateTime? date = o.Date;
            string info = "";
            if (o.Holiday != null)
                l = leyning.ForHoliday(o.Holiday, o.Date, Custom);
            else
            {
                if (cboParasha.SelectedItem is not ParashaItem item || cboYear.SelectedIndex < 0) return;
                int year = cboYear.SelectedIndex + 1;
                int calendarYear = Triennial.CycleYear(ParashaCalendar.Hc.GetYear(o.Date));
                var shabbat = ParashaCalendar.ForShabbat(o.Date, Israel);

                // Date-specific details (special maftir/haftarah) apply only when the selection matches the calendar.
                bool matches = item.Portions.SequenceEqual(o.Portions!) && year == calendarYear;
                if (!matches)
                {
                    date = null;
                    info = $"Custom selection. The calendar reading for {o.Date:MMM d} is {o.Name}, Year {calendarYear}.";
                }
                // Any reading year with the chosen cycle year gives the right aliyot; the pattern for
                // sometimes-combined portions comes from this date's 3-year cycle.
                int readingYear = shabbat.ReadingYear - Triennial.CycleYear(shabbat.ReadingYear) + year;
                l = leyning.ForShabbat(item.Portions, readingYear, Israel, date, Custom);
            }

            lblInfo.Text = info;
            lblInfo.Visible = info.Length > 0;
            // Phonetic MAM is read (a book at a time) only once a transliteration is chosen.
            var options = Options;
            current = ReadingDocument.Build(l, text, date, Israel, chkUpper.Checked, Custom,
                                            shevaNa, (ShevaNaStyle)Math.Max(0, cboShevaNa.SelectedIndex),
                                            options.Transliteration == TransliterationStyle.Off ? null : phonetic);
            rtbReading.Rtf = RtfRenderer.Render(current, options);
        }

        void dtpShabbat_ValueChanged(object? sender, EventArgs e)
        {
            if (!updating) GoToDate(dtpShabbat.Value);
        }

        void btnUpcoming_Click(object? sender, EventArgs e) => GoToDate(DateTime.Today, shabbatOrFestival: true);

        void cboOccasion_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!updating) ShowOccasion();
        }

        void btnPrev_Click(object? sender, EventArgs e) => Step(-1);

        void btnNext_Click(object? sender, EventArgs e) => Step(+1);

        void Step(int delta)
        {
            if (Selected is not Occasion o) return;
            int i = cboOccasion.SelectedIndex + delta;
            if (i >= 0 && i < cboOccasion.Items.Count) { cboOccasion.SelectedIndex = i; return; }
            // Off the end of the list: rebuild it around the neighbouring day and continue from there.
            FillOccasions(o.Date.AddDays(delta));
            var items = cboOccasion.Items.Cast<Occasion>().ToList();
            var next = delta > 0 ? items.FirstOrDefault(x => x.Date > o.Date) : items.LastOrDefault(x => x.Date < o.Date);
            if (next != null) cboOccasion.SelectedItem = next;
        }

        void chkIsrael_CheckedChanged(object? sender, EventArgs e)
        {
            if (updating) return;
            var date = Selected?.Date ?? DateTime.Today;
            FillOccasions(date);
            GoToDate(date);
        }

        void selection_Changed(object? sender, EventArgs e) => RenderReading();

        void display_Changed(object? sender, EventArgs e) => RenderReading();

        /// <summary>The large sheva is drawn by the bundled font, so choosing it selects that font.</summary>
        void cboShevaNa_SelectedIndexChanged(object? sender, EventArgs e)
        {
            bool large = (ShevaNaStyle)cboShevaNa.SelectedIndex is ShevaNaStyle.LargeSheva or ShevaNaStyle.LargeShevaAndColor;
            if (!updating && large && HebrewFontName != ShevaNa.FontFamily)
            {
                updating = true;
                SelectFont(ShevaNa.FontFamily);
                updating = false;
            }
            RenderReading();
        }

        string HebrewFontName => (cboFont.SelectedItem as FontItem)?.Name ?? "";

        void SelectFont(string name) =>
            cboFont.SelectedItem = cboFont.Items.Cast<FontItem>().FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        /// <summary>A font in the Font list: its name, and whether it has the trope marks.</summary>
        sealed record FontItem(string Name, bool Trope)
        {
            public override string ToString() => Trope ? Name : $"{Name} (no trope)";
        }

        const uint FR_PRIVATE = 0x10;

        [System.Runtime.InteropServices.DllImport("gdi32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern int AddFontResourceEx(string name, uint flags, IntPtr reserved);

        [System.Runtime.InteropServices.DllImport("gdi32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern bool RemoveFontResourceEx(string name, uint flags, IntPtr reserved);

        string SuggestedFileName =>
            string.Concat($"{current?.Title.Replace(" — ", " - ")} {Selected?.Date:yyyy-MM-dd}.pdf".Split(Path.GetInvalidFileNameChars()));

        void btnPdf_Click(object? sender, EventArgs e)
        {
            if (current is not ReadingDocument doc) return;
            using var dlg = new SaveFileDialog { Filter = "PDF files (*.pdf)|*.pdf", FileName = SuggestedFileName };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                UseWaitCursor = true;
                PdfRenderer.Save(doc, dlg.FileName, Options);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Could not save PDF", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                UseWaitCursor = false;
            }

            if (MessageBox.Show(this, $"Saved {Path.GetFileName(dlg.FileName)}.\n\nOpen it now?", "PDF saved",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
        }

        HelpForm? help;

        void btnHelp_Click(object? sender, EventArgs e) => ShowHelp();

        void ShowHelp()
        {
            if (help == null || help.IsDisposed) help = new HelpForm();
            help.Show();
            help.Activate();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F1) { ShowHelp(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        void btnPrint_Click(object? sender, EventArgs e)
        {
            if (current is not ReadingDocument doc) return;
            using var printer = new ReadingPrinter(doc, Options);
            using var dlg = new PrintDialog
            {
                Document = printer.Document,
                AllowSomePages = true,
                UseEXDialog = true,
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                UseWaitCursor = true;
                printer.Document.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Could not print", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }
    }
}
