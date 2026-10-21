namespace LeynerLearner.Rendering;

public enum VerseLayout
{
    /// <summary>Each verse starts on its own line.</summary>
    VersePerLine,
    /// <summary>Verses run on as one paragraph per aliyah, each starting where the previous one ends.</summary>
    Continuous,
    /// <summary>Like a Torah scroll: justified fixed-width lines, petuchah/setumah gaps and the song layouts.</summary>
    Tikkun,
}

/// <summary>Display settings shared by the screen view, PDF and printing.</summary>
/// <param name="TikkunVowels">Tikkun layout: show vowels (a scroll has none).</param>
/// <param name="TikkunTrope">Tikkun layout: show trope marks (a scroll has none).</param>
public record RenderOptions(string HebrewFont, float Size, bool English, VerseLayout Layout,
                            bool TikkunVowels = false, bool TikkunTrope = false);
