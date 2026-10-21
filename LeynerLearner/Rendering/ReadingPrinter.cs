using System.Drawing.Printing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using LeynerLearner.Core;

namespace LeynerLearner.Rendering;

/// <summary>
/// Prints a reading with exactly the PDF layout: QuestPDF renders each page to an image (so Hebrew
/// vowels and trope marks are shaped as in the PDF), and a PrintDocument sends the images to the printer.
/// </summary>
public sealed class ReadingPrinter : IDisposable
{
    const int RenderDpi = 300;

    readonly ReadingDocument doc;
    readonly RenderOptions options;
    List<byte[]> pages = [];
    int next, last;

    public PrintDocument Document { get; } = new();

    public ReadingPrinter(ReadingDocument doc, RenderOptions options)
    {
        this.doc = doc;
        this.options = options;
        Document.DocumentName = doc.Title;
        Document.PrinterSettings.MinimumPage = 1;
        Document.PrinterSettings.MaximumPage = 999;   // the real count is known only after layout; BeginPrint clamps
        Document.PrinterSettings.FromPage = 1;
        Document.PrinterSettings.ToPage = 999;
        Document.BeginPrint += BeginPrint;
        Document.PrintPage += PrintPage;
    }

    List<byte[]> Render()
    {
        // Lay the reading out on the printer's paper (PaperSize is in hundredths of an inch; points are 1/72").
        var ps = Document.DefaultPageSettings;
        float w = ps.PaperSize.Width * 0.72f, h = ps.PaperSize.Height * 0.72f;
        if (ps.Landscape) (w, h) = (h, w);
        return PdfRenderer.Create(doc, options, new PageSize(w, h))
            .GenerateImages(new ImageGenerationSettings { RasterDpi = RenderDpi })
            .ToList();
    }

    void BeginPrint(object? sender, PrintEventArgs e)
    {
        pages = Render();
        var settings = Document.PrinterSettings;
        bool range = settings.PrintRange == PrintRange.SomePages;
        next = range ? Math.Max(1, settings.FromPage) - 1 : 0;
        last = range ? Math.Min(pages.Count, settings.ToPage) - 1 : pages.Count - 1;
    }

    void PrintPage(object? sender, PrintPageEventArgs e)
    {
        if (next > last || next >= pages.Count) { e.HasMorePages = false; return; }

        using var image = System.Drawing.Image.FromStream(new MemoryStream(pages[next]));
        // Graphics origin is the printable area's corner; shift so the image covers the whole sheet.
        var bounds = e.PageBounds;
        e.Graphics!.DrawImage(image, new RectangleF(
            -e.PageSettings.HardMarginX, -e.PageSettings.HardMarginY, bounds.Width, bounds.Height));

        next++;
        e.HasMorePages = next <= last && next < pages.Count;
    }

    public void Dispose() => Document.Dispose();
}
