using System.Diagnostics;
using LeynerLearner.Core;
using LeynerLearner.Rendering;

namespace LeynerLearner
{
    public partial class Form1 : Form
    {
        /// <summary>A parashah (or combined pair) in the drop-down.</summary>
        sealed record ParashaItem(int[] Portions)
        {
            public override string ToString() => ParashaCalendar.Name(Portions);
        }

        // Fonts with good vowel + trope coverage, in order of preference.
        static readonly string[] PreferredFonts =
            ["SBL Hebrew", "Ezra SIL", "Taamey Frank CLM", "Frank Ruehl CLM", "Noto Serif Hebrew", "Times New Roman"];

        // How far around the chosen date the Reading list extends.
        static readonly TimeSpan ListBefore = TimeSpan.FromDays(60), ListAfter = TimeSpan.FromDays(400);

        readonly string dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
        ScriptureText text = null!;
        LeyningData leyning = null!;
        IReadOnlyList<Song> songs = [];
        ShevaNa shevaNa = null!;

        DateTime listFrom, listTo;    // range covered by cboOccasion
        ReadingDocument? current;     // what is displayed (and saved/printed)
        bool updating;

        public Form1()
        {
            InitializeComponent();
        }

        Occasion? Selected => cboOccasion.SelectedItem as Occasion;
        bool Israel => chkIsrael.Checked;
        HaftarahCustom Custom => (HaftarahCustom)cboHaftarah.SelectedIndex;
        RenderOptions Options => new(cboFont.SelectedItem as string ?? "Times New Roman", (float)numSize.Value,
                                     chkEnglish.Checked, (VerseLayout)Math.Max(0, cboLayout.SelectedIndex),
                                     chkVowels.Checked, chkTrope.Checked);

        void Form1_Load(object? sender, EventArgs e)
        {
            text = new ScriptureText(dataDir);
            leyning = new LeyningData(Path.Combine(dataDir, "Hebcal"));
            songs = Songs.Load(Path.Combine(dataDir, "MAM", "songs.json"), text);
            shevaNa = ShevaNa.Load(Path.Combine(dataDir, "MAM", "sheva-na.json"));

            // The bundled font (with the large sounded-sheva glyph) is loaded for this process only.
            string bundledFont = Path.Combine(dataDir, "Fonts", "TropeTrainerHebrew.ttf");
            AddFontResourceEx(bundledFont, FR_PRIVATE, IntPtr.Zero);
            PdfRenderer.RegisterFont(bundledFont);

            updating = true;
            // Weekly portions in Torah order, each combined pair listed right after its first half.
            for (int p = 0; p < ParashaCalendar.Portions.Length - 1; p++)   // V'Zot HaBerachah is read on Simchat Torah
            {
                cboParasha.Items.Add(new ParashaItem([p]));
                if (ParashaCalendar.SometimesCombined.Contains(p)) cboParasha.Items.Add(new ParashaItem([p, p + 1]));
            }
            cboHaftarah.Items.AddRange(Enum.GetNames<HaftarahCustom>());
            cboHaftarah.SelectedIndex = (int)HaftarahCustom.Ashkenazi;
            cboLayout.SelectedIndex = (int)VerseLayout.VersePerLine;

            var installed = FontFamily.Families.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            installed.Add(ShevaNa.FontFamily);
            foreach (var name in installed.Order()) cboFont.Items.Add(name);
            cboFont.SelectedItem = PreferredFonts.FirstOrDefault(installed.Contains) ?? "Times New Roman";
            cboShevaNa.SelectedIndex = (int)ShevaNaStyle.Color;
            updating = false;

            GoToDate(DateTime.Today, shabbatOrFestival: true);
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
            current = ReadingDocument.Build(l, text, date, Israel, chkUpper.Checked, Custom, songs,
                                            shevaNa, (ShevaNaStyle)Math.Max(0, cboShevaNa.SelectedIndex));
            ShowDocument(current, Options);
        }

        /// <summary>
        /// Shows the reading: as rich text, or for the Tikkun layout as rendered pages (the justified scroll
        /// lines and song layouts are drawn exactly as in the PDF and printout).
        /// </summary>
        void ShowDocument(ReadingDocument doc, RenderOptions options)
        {
            bool tikkun = options.Layout == VerseLayout.Tikkun;
            chkVowels.Enabled = chkTrope.Enabled = tikkun;

            foreach (Control c in pnlPages.Controls) { (c as PictureBox)?.Image?.Dispose(); c.Dispose(); }
            pnlPages.Controls.Clear();
            if (!tikkun)
            {
                rtbReading.Rtf = RtfRenderer.Render(doc, options);
                pnlPages.Visible = false;
                rtbReading.Visible = true;
                return;
            }

            UseWaitCursor = true;
            try
            {
                var settings = new QuestPDF.Infrastructure.ImageGenerationSettings { RasterDpi = DeviceDpi };
                foreach (var png in QuestPDF.Fluent.GenerateExtensions.GenerateImages(PdfRenderer.Create(doc, options), settings))
                {
                    var image = Image.FromStream(new MemoryStream(png));
                    pnlPages.Controls.Add(new PictureBox { Image = image, SizeMode = PictureBoxSizeMode.AutoSize, Margin = new Padding(0, 0, 0, 12) });
                }
            }
            finally
            {
                UseWaitCursor = false;
            }
            rtbReading.Visible = false;
            pnlPages.Visible = true;
            pnlPages.AutoScrollPosition = Point.Empty;
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
            if (!updating && cboShevaNa.SelectedIndex == (int)ShevaNaStyle.LargeSheva && HebrewFontName != ShevaNa.FontFamily)
            {
                updating = true;
                cboFont.SelectedItem = ShevaNa.FontFamily;
                updating = false;
            }
            RenderReading();
        }

        string HebrewFontName => cboFont.SelectedItem as string ?? "";

        const uint FR_PRIVATE = 0x10;

        [System.Runtime.InteropServices.DllImport("gdi32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern int AddFontResourceEx(string name, uint flags, IntPtr reserved);

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
