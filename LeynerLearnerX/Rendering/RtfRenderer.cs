using System.Text;
using LeynerLearner.Core;

namespace LeynerLearner.Rendering;

/// <summary>
/// Builds RTF for the RichTextBox. Hebrew paragraphs are marked right-to-left (\rtlpar) so verse
/// numbers, punctuation and trope marks lay out correctly; headings and English are left-to-right.
/// </summary>
public static class RtfRenderer
{
    public static string Render(ReadingDocument doc, RenderOptions o)
    {
        int heb = (int)Math.Round(o.Size * 2);                 // RTF sizes are in half-points
        int small = (int)Math.Round(Math.Max(8, o.Size * 0.5f) * 2);
        int eng = (int)Math.Round(Math.Max(9, o.Size * 0.6f) * 2);
        int head = (int)Math.Round(Math.Max(10, o.Size * 0.7f) * 2);
        string hebrewPara = $@"\pard\li240\ri240\rtlpar\sl{heb * 15}\slmult0";   // line spacing 1.5x
        string englishPara = @"\pard\li240\ri240\ltrpar\ql\f1\cf2\fs" + eng;

        var sb = new StringBuilder();
        sb.Append(@"{\rtf1\ansi\ansicpg1252\deff1");
        sb.Append(@"{\fonttbl{\f0\fnil\fcharset177 ").Append(Escape(o.HebrewFont)).Append(@";}{\f1\fswiss\fcharset0 Segoe UI;}}");
        sb.Append(@"{\colortbl;\red0\green0\blue0;\red120\green120\blue120;\red40\green80\blue160;")
          .Append($@"\red{ShevaNaColor.R}\green{ShevaNaColor.G}\blue{ShevaNaColor.B};}}");

        sb.Append(@"\pard\li240\ri240\ltrpar\ql\sa40\f1\fs").Append(head + 8).Append(@"\b ").Append(Escape(doc.Title)).Append(@"\b0\par");
        sb.Append(@"\pard\li240\ri240\ltrpar\ql\sa120\f1\cf2\fs").Append(eng).Append(' ').Append(Escape(doc.Subtitle)).Append(@"\cf1\par");

        // Verse number, Hebrew text and paragraph mark of one verse, inside a right-to-left paragraph.
        // Non-breaking spaces keep the number on the same line as the verse's first word.
        void HebrewVerse(Verse v)
        {
            sb.Append(@"{\rtlch\af1\afs").Append(small).Append(@"\cf2 ").Append(Escape($"{v.Chapter}:{v.Number}  ")).Append('}');
            sb.Append(@"{\rtlch\af0\afs").Append(heb).Append(@"\f0\fs").Append(heb).Append(' ').Append(EscapeHebrew(v.Hebrew));
            if (v.ParagraphMark != null) sb.Append(@" {\cf2 ").Append(Escape(v.ParagraphMark)).Append('}');
            sb.Append('}');
        }

        foreach (var section in doc.Sections)
        {
            sb.Append(@"\pard\li240\ri240\ltrpar\ql\sb240\sa80\f1\fs").Append(head).Append(@"\cf3\b ")
              .Append(Escape(section.Heading)).Append(@"\b0\cf2 ").Append(Escape("  —  " + section.Range)).Append(@"\cf1\par");

            if (section.Verses.Count == 0)
                sb.Append(englishPara).Append(@" (text not available)\cf1\par");

            if (o.Layout == VerseLayout.Continuous)
            {
                if (section.Verses.Count == 0) continue;
                // One justified paragraph per aliyah; verses separated by a space.
                sb.Append(hebrewPara).Append(@"\qj\sa120 ");
                foreach (var v in section.Verses) { HebrewVerse(v); sb.Append(' '); }
                sb.Append(@"\par");
                var withEnglish = section.Verses.Where(v => v.English != null).ToList();
                if (o.English && withEnglish.Count > 0)
                {
                    sb.Append(englishPara).Append(@"\sa160 ");
                    foreach (var v in withEnglish)
                        sb.Append(@"{\fs").Append(small).Append(' ').Append(Escape($"{v.Chapter}:{v.Number}")).Append(@"} ")
                          .Append(Escape(v.English!)).Append(' ');
                    sb.Append(@"\cf1\par");
                }
                continue;
            }

            foreach (var v in section.Verses)
            {
                sb.Append(hebrewPara).Append(@"\qr\sa60 ");
                HebrewVerse(v);
                sb.Append(@"\par");
                if (o.English && v.English != null)
                    sb.Append(englishPara).Append(@"\sa160 ").Append(Escape(v.English)).Append(@"\cf1\par");
            }
        }
        sb.Append(@"\pard\li240\ri240\ltrpar\ql\sb360\f1\cf2\fs16 ").Append(Escape(doc.Attribution)).Append(@"\cf1\par");
        sb.Append('}');
        return sb.ToString();
    }

    /// <summary>Color for letters with a sounded sheva (sheva na).</summary>
    public static readonly (byte R, byte G, byte B) ShevaNaColor = (200, 30, 45);

    /// <summary>Hebrew text with letters marked by <see cref="ShevaNa.Marker"/> drawn in <see cref="ShevaNaColor"/>.</summary>
    static string EscapeHebrew(string s) =>
        string.Concat(ShevaNa.Runs(s).Select(r => r.Sounded ? @"{\cf4 " + Escape(r.Text) + "}" : Escape(r.Text)));

    static string Escape(string s)
    {
        var sb = new StringBuilder(s.Length * 2);
        foreach (char c in s)
        {
            if (c is '\\' or '{' or '}') sb.Append('\\').Append(c);
            else if (c < 128) sb.Append(c);
            else sb.Append(@"\u").Append((short)c).Append('?');
        }
        return sb.ToString();
    }
}
