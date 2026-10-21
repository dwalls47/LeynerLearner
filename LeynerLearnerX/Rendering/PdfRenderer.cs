using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using LeynerLearner.Core;

namespace LeynerLearner.Rendering;

/// <summary>Writes a reading to PDF. QuestPDF shapes Hebrew (vowels and trope marks) with HarfBuzz.</summary>
public static partial class PdfRenderer
{
    static PdfRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        // The font is chosen from the fonts installed on this PC (same list as the on-screen view).
        QuestPDF.Settings.UseSystemFonts = true;
    }

    /// <summary>Makes a font file that isn't installed (the bundled TropeTrainer Hebrew) available to PDFs.</summary>
    public static void RegisterFont(string path)
    {
        using var stream = File.OpenRead(path);
        QuestPDF.Drawing.FontManager.RegisterFontFromStream(stream);
    }

    const float Margin = 54;   // 0.75 inch

    public static void Save(ReadingDocument doc, string path, RenderOptions options) =>
        Create(doc, options).GeneratePdf(path);

    static readonly string ShevaNaHex =
        $"#{RtfRenderer.ShevaNaColor.R:X2}{RtfRenderer.ShevaNaColor.G:X2}{RtfRenderer.ShevaNaColor.B:X2}";

    /// <summary>Hebrew text as spans; letters marked with <see cref="ShevaNa.Marker"/> (sounded sheva) in color.
    /// Each colored span is a whole letter with its marks, so shaping is unaffected.</summary>
    static void HebrewSpans(TextDescriptor t, string text, string font, float size, float? lineHeight = null)
    {
        foreach (var (run, sounded) in ShevaNa.Runs(text))
        {
            var span = t.Span(run).FontFamily(font).FontSize(size);
            if (lineHeight is float lh) span = span.LineHeight(lh);
            if (sounded) span.FontColor(ShevaNaHex);
        }
    }

    /// <summary>Draws tikkun lines: words spread to both margins on justified lines, fixed setumah gaps,
    /// and song lines with their cells right, (center,) left.</summary>
    static void TikkunColumn(ColumnDescriptor c, List<TikkunLine> lines, string font, float size, float spacing)
    {
        void Word(RowDescriptor row, string w) => row.AutoItem().Text(t => HebrewSpans(t, w, font, size));

        foreach (var line in lines)
        {
            switch (line)
            {
                case BlankLine:
                    c.Item().Height(size * 1.3f + spacing);
                    break;
                case ProseLine p:
                    c.Item().PaddingBottom(spacing).ContentFromRightToLeft().Row(row =>
                    {
                        for (int i = 0; i < p.Items.Count; i++)
                        {
                            var item = p.Items[i];
                            if (item.Word == null) { row.ConstantItem(item.Gap); continue; }
                            if (i > 0 && p.Items[i - 1].Word != null)
                                if (p.Justify) row.RelativeItem(); else row.ConstantItem(size * 0.3f);
                            Word(row, item.Word);
                        }
                        if (!p.Justify) row.RelativeItem();   // the rest of the line stays open
                    });
                    break;
                case SongLine s:
                    // Right piece flush right, left piece flush left, a center piece midway; the space between
                    // is shared equally (the column is sized so the longest song line fits).
                    c.Item().PaddingBottom(spacing).ContentFromRightToLeft().Row(row =>
                    {
                        for (int i = 0; i < s.Cells.Count; i++)
                        {
                            if (i > 0) row.RelativeItem();
                            row.AutoItem().Text(t => HebrewSpans(t, s.Cells[i].Text, font, size));
                        }
                    });
                    break;
            }
        }
    }

    /// <summary>
    /// Measures text in points for tikkun line breaking. Uses GDI directly rather than GDI+
    /// (System.Drawing), because GDI+ can't load CFF-based OpenType fonts such as Frank Ruehl CLM and
    /// silently measures with a substitute.
    /// </summary>
    internal sealed partial class TextMeasurer : IDisposable
    {
        const int Scale = 50;   // the font is created at Scale x the point size, for precision
        readonly IntPtr dc, font, oldFont;

        public TextMeasurer(string family, float size)
        {
            dc = CreateCompatibleDC(IntPtr.Zero);
            font = CreateFontW(-(int)Math.Round(size * Scale), 0, 0, 0, 400, 0, 0, 0, 1 /* DEFAULT_CHARSET */,
                               0, 0, 4 /* ANTIALIASED_QUALITY */, 0, family);
            oldFont = SelectObject(dc, font);
        }

        public float Width(string s)
        {
            s = s.Replace(ShevaNa.Marker.ToString(), "");   // the sounded-sheva marker is never drawn
            return s.Length == 0 ? 0 : GetTextExtentPoint32W(dc, s, s.Length, out var sz) ? sz.Cx / (float)Scale : 0;
        }

        public void Dispose()
        {
            SelectObject(dc, oldFont);
            DeleteObject(font);
            DeleteDC(dc);
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        struct Size { public int Cx, Cy; }

        [System.Runtime.InteropServices.LibraryImport("gdi32.dll", StringMarshalling = System.Runtime.InteropServices.StringMarshalling.Utf16)]
        private static partial IntPtr CreateFontW(int height, int width, int escapement, int orientation, int weight,
            uint italic, uint underline, uint strikeOut, uint charSet, uint outPrecision, uint clipPrecision,
            uint quality, uint pitchAndFamily, string face);

        [System.Runtime.InteropServices.LibraryImport("gdi32.dll")]
        private static partial IntPtr CreateCompatibleDC(IntPtr dc);

        [System.Runtime.InteropServices.LibraryImport("gdi32.dll")]
        private static partial IntPtr SelectObject(IntPtr dc, IntPtr obj);

        [System.Runtime.InteropServices.LibraryImport("gdi32.dll", StringMarshalling = System.Runtime.InteropServices.StringMarshalling.Utf16)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static partial bool GetTextExtentPoint32W(IntPtr dc, string s, int count, out Size size);

        [System.Runtime.InteropServices.LibraryImport("gdi32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static partial bool DeleteObject(IntPtr obj);

        [System.Runtime.InteropServices.LibraryImport("gdi32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static partial bool DeleteDC(IntPtr dc);
    }

    /// <param name="pageSize">Paper size; defaults to US Letter (printing passes the printer's paper).</param>
    public static Document Create(ReadingDocument doc, RenderOptions o, PageSize? pageSize = null)
    {
        const string Latin = "Segoe UI";
        string hebrewFont = o.HebrewFont;
        float size = o.Size;
        float small = Math.Max(7, size * 0.45f), eng = Math.Max(9, size * 0.55f), head = Math.Max(10, size * 0.65f);

        // Verse number, Hebrew text and paragraph mark of one verse. Non-breaking spaces keep the
        // number on the same line as the verse's first word.
        void HebrewVerse(TextDescriptor t, Verse v)
        {
            t.Span($"{v.Chapter}:{v.Number}  ").FontFamily(Latin).FontSize(small).FontColor(Colors.Grey.Medium);
            HebrewSpans(t, v.Hebrew, hebrewFont, size, lineHeight: 1.5f);
            if (v.ParagraphMark != null)
                t.Span("  " + v.ParagraphMark).FontFamily(hebrewFont).FontSize(size).FontColor(Colors.Grey.Medium);
        }

        // Tikkun: lay out the Torah sections as scroll lines now (text is measured with GDI+).
        var paper = pageSize ?? PageSizes.Letter;
        var tikkun = new Dictionary<ReadingSection, List<TikkunLine>>();
        float columnWidth = 0;
        if (o.Layout == VerseLayout.Tikkun)
        {
            using var measurer = new TextMeasurer(hebrewFont, size);
            var layout = new TikkunLayout(measurer.Width, paper.Width - 2 * Margin, size, doc.Songs, o.TikkunVowels, o.TikkunTrope);
            foreach (var s in doc.Sections.Where(s => !s.IsHaftarah)) tikkun[s] = layout.Layout(s);
            columnWidth = layout.ColumnWidth;
        }

        return Document.Create(container => container.Page(page =>
        {
            page.Size(paper);
            page.Margin(Margin);
            page.DefaultTextStyle(t => t.FontFamily(Latin).FontSize(eng));

            page.Header().PaddingBottom(8).Column(col =>
            {
                col.Item().Text(doc.Title).FontSize(head + 4).Bold();
                col.Item().Text(doc.Subtitle).FontColor(Colors.Grey.Darken1);
            });

            page.Content().Column(col =>
            {
                foreach (var section in doc.Sections)
                {
                    col.Item().PaddingTop(14).PaddingBottom(4).Text(t =>
                    {
                        t.Span(section.Heading).FontSize(head).Bold().FontColor(Colors.Blue.Darken3);
                        t.Span("  —  " + section.Range).FontSize(head).FontColor(Colors.Grey.Darken1);
                    });
                    if (section.Verses.Count == 0)
                    {
                        col.Item().Text("(text not available)").FontColor(Colors.Grey.Medium);
                        continue;
                    }

                    if (tikkun.TryGetValue(section, out var lines))
                    {
                        col.Item().AlignCenter().Width(columnWidth).Column(c => TikkunColumn(c, lines, hebrewFont, size,
                            spacing: size * (o.TikkunVowels || o.TikkunTrope ? 0.45f : 0.3f)));
                        continue;
                    }

                    // Continuous (also used for haftarot in the Tikkun layout, since they aren't read from a scroll).
                    if (o.Layout is VerseLayout.Continuous or VerseLayout.Tikkun)
                    {
                        // One justified paragraph per aliyah; each verse follows the previous one.
                        col.Item().PaddingBottom(6).ContentFromRightToLeft().Text(t =>
                        {
                            t.Justify();
                            foreach (var v in section.Verses) { HebrewVerse(t, v); t.Span("  ").FontSize(size); }
                        });
                        var withEnglish = section.Verses.Where(v => v.English != null).ToList();
                        if (o.English && withEnglish.Count > 0)
                            col.Item().PaddingBottom(8).Text(t =>
                            {
                                foreach (var v in withEnglish)
                                {
                                    t.Span($"{v.Chapter}:{v.Number} ").FontSize(small).FontColor(Colors.Grey.Medium);
                                    t.Span(v.English + " ").FontSize(eng).FontColor(Colors.Grey.Darken2);
                                }
                            });
                        continue;
                    }

                    foreach (var v in section.Verses)
                    {
                        col.Item().PaddingBottom(4).ContentFromRightToLeft().Text(t =>
                        {
                            t.AlignRight();
                            HebrewVerse(t, v);
                        });
                        if (o.English && v.English != null)
                            col.Item().PaddingBottom(8).Text(v.English).FontSize(eng).FontColor(Colors.Grey.Darken2);
                    }
                }
            });

            page.Footer().Column(col =>
            {
                col.Item().AlignCenter().Text(doc.Attribution).FontSize(7).FontColor(Colors.Grey.Medium);
                col.Item().AlignCenter().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Medium));
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
            });
        }));
    }
}
