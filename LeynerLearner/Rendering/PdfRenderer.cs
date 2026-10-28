using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using LeynerLearner.Core;

namespace LeynerLearner.Rendering;

/// <summary>Writes a reading to PDF. QuestPDF shapes Hebrew (vowels and trope marks) with HarfBuzz.</summary>
public static class PdfRenderer
{
    static PdfRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        // The font is chosen from the fonts installed on this PC (same list as the on-screen view).
        QuestPDF.Settings.UseSystemFonts = true;
    }

    /// <summary>Makes a font file that isn't installed (the bundled Hebrew Sheva Na) available to PDFs.</summary>
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

        return Document.Create(container => container.Page(page =>
        {
            page.Size(pageSize ?? PageSizes.Letter);
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

                    // One justified paragraph per aliyah; each verse follows the previous one.
                    col.Item().PaddingBottom(6).ContentFromRightToLeft().Text(t =>
                    {
                        t.Justify();
                        foreach (var v in section.Verses) { HebrewVerse(t, v); t.Span("  ").FontSize(size); }
                    });
                    var withPhonetic = section.Verses.Where(v => v.Phonetic != null).ToList();
                    if (o.Transliteration != TransliterationStyle.Off && withPhonetic.Count > 0)
                        col.Item().PaddingBottom(6).Text(t =>
                        {
                            foreach (var v in withPhonetic)
                            {
                                t.Span($"{v.Chapter}:{v.Number} ").FontSize(small).FontColor(Colors.Grey.Medium);
                                foreach (var w in v.Phonetic!.Where(w => w == null || w.Sephardic.Count > 0))
                                {
                                    // A sounded sheva as a superscript e, the stressed syllable underlined, "…" for a word with none.
                                    if (w == null) t.Span("…").FontSize(eng);
                                    else
                                        foreach (var r in w.For(o.Transliteration))
                                        {
                                            var span = t.Span(r.Text).FontSize(eng);
                                            if (r.SoundedSheva) span = span.Superscript();
                                            if (r.Stressed) span.Underline();
                                        }
                                    t.Span(" ").FontSize(eng);
                                }
                            }
                        });
                    var withEnglish = section.Verses.Where(v => v.EnglishFor(o.English) != null).ToList();
                    if (withEnglish.Count > 0)
                        col.Item().PaddingBottom(8).Text(t =>
                        {
                            foreach (var v in withEnglish)
                            {
                                t.Span($"{v.Chapter}:{v.Number} ").FontSize(small).FontColor(Colors.Grey.Medium);
                                t.Span(v.EnglishFor(o.English) + " ").FontSize(eng).FontColor(Colors.Grey.Darken2);
                            }
                        });
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
