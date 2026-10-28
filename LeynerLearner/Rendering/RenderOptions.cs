using LeynerLearner.Core;

namespace LeynerLearner.Rendering;

/// <summary>Display settings shared by the screen view, PDF and printing.</summary>
public record RenderOptions(string HebrewFont, float Size, EnglishVersion English,
                            TransliterationStyle Transliteration = TransliterationStyle.Off);
