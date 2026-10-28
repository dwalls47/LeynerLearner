using System.Runtime.InteropServices;

namespace LeynerLearner.Rendering;

/// <summary>
/// Which fonts can show the Hebrew text: Windows reports the characters each font contains (GetFontUnicodeRanges),
/// so the check is of the font itself, not its name or the code pages it claims.
/// </summary>
public static partial class HebrewFonts
{
    /// <param name="Letters">All 27 Hebrew letters (U+05D0-U+05EA).</param>
    /// <param name="Vowels">The vowel points, dagesh and shin/sin dots.</param>
    /// <param name="TropeMarks">How many of the 31 cantillation marks (U+0591-U+05AF) it has.</param>
    public record Coverage(bool Letters, bool Vowels, int TropeMarks)
    {
        /// <summary>Can show pointed Hebrew (letters and vowels).</summary>
        public bool Hebrew => Letters && Vowels;
        public bool Trope => TropeMarks == 31;
    }

    static readonly int[] VowelPoints = [.. Enumerable.Range(0x05B0, 10), 0x05BB, 0x05BC, 0x05C1, 0x05C2];

    /// <summary>The font's Hebrew coverage, or null if Windows has no font of that name (it would substitute another).</summary>
    public static Coverage? Check(string family)
    {
        IntPtr dc = CreateCompatibleDC(IntPtr.Zero);
        IntPtr font = CreateFontW(-48, 0, 0, 0, 400, 0, 0, 0, 1 /* DEFAULT_CHARSET */, 0, 0, 0, 0, family);
        IntPtr old = SelectObject(dc, font);
        try
        {
            // Windows keeps only the first 31 characters of a font name.
            var face = new char[64];
            int n = GetTextFaceW(dc, face.Length, face);
            string expected = family.Length > 31 ? family[..31] : family;
            if (n <= 0 || !new string(face, 0, n - 1).Equals(expected, StringComparison.OrdinalIgnoreCase)) return null;

            uint size = GetFontUnicodeRanges(dc, IntPtr.Zero);
            if (size == 0) return null;
            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                GetFontUnicodeRanges(dc, buffer);
                // GLYPHSET: cbThis, flAccel, cGlyphsSupported, cRanges (4 bytes each), then WCRANGE { wcLow, cGlyphs }.
                int ranges = Marshal.ReadInt32(buffer, 12);
                var covered = new List<(int Low, int High)>(ranges);
                for (int i = 0; i < ranges; i++)
                {
                    int low = (ushort)Marshal.ReadInt16(buffer, 16 + i * 4);
                    int count = (ushort)Marshal.ReadInt16(buffer, 18 + i * 4);
                    covered.Add((low, low + count - 1));
                }
                bool Has(int c) => covered.Any(r => c >= r.Low && c <= r.High);
                return new Coverage(Enumerable.Range(0x05D0, 27).All(Has), VowelPoints.All(Has),
                                    Enumerable.Range(0x0591, 31).Count(Has));
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }
        finally
        {
            SelectObject(dc, old);
            DeleteObject(font);
            DeleteDC(dc);
        }
    }

    [LibraryImport("gdi32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr CreateFontW(int height, int width, int escapement, int orientation, int weight,
        uint italic, uint underline, uint strikeOut, uint charSet, uint outPrecision, uint clipPrecision,
        uint quality, uint pitchAndFamily, string face);

    [LibraryImport("gdi32.dll")]
    private static partial IntPtr CreateCompatibleDC(IntPtr dc);

    [LibraryImport("gdi32.dll")]
    private static partial IntPtr SelectObject(IntPtr dc, IntPtr obj);

    [LibraryImport("gdi32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int GetTextFaceW(IntPtr dc, int count, [Out] char[] face);

    [LibraryImport("gdi32.dll")]
    private static partial uint GetFontUnicodeRanges(IntPtr dc, IntPtr glyphSet);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteObject(IntPtr obj);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteDC(IntPtr dc);
}
