namespace PptxGenerator.Prompt;

/// <summary>
/// Defines the supported elements, attributes, and hierarchy for one complete SlideML Page document.
/// </summary>
public static class SlideMlCompleteDocumentSchema
{
    private static readonly IReadOnlyList<SlideMlElementSchema> ElementsValue =
    [
        new("Page", ["Background"], ["Panel", "Rect", "TextElement", "Image"]),
        new("Panel", ["Id", "X", "Y", "Width", "Height", "Padding", "Background", "Layout", "Gap", "Margin", "HorizontalAlignment", "VerticalAlignment", "Opacity"], ["Panel", "Rect", "TextElement", "Image", "Fill"]),
        new("Rect", ["Id", "X", "Y", "Width", "Height", "Fill", "Stroke", "StrokeThickness", "CornerRadius", "StrokeDashArray", "Margin", "HorizontalAlignment", "VerticalAlignment", "Opacity"], ["Fill", "Stroke"]),
        new("TextElement", ["Id", "X", "Y", "Width", "Height", "Text", "FontName", "FontSize", "IsBold", "IsItalic", "Foreground", "TextAlignment", "HorizontalAlignment", "VerticalAlignment", "Opacity", "Margin"], ["Span"]),
        new("Image", ["Id", "X", "Y", "Width", "Height", "Source", "Stretch", "HorizontalAlignment", "VerticalAlignment", "Opacity", "Margin"], []),
        new("Span", ["Text", "FontSize", "FontName", "Foreground", "IsBold", "IsItalic", "TextDecoration"], []),
        new("Fill", [], ["LinearGradient"]),
        new("Stroke", [], ["LinearGradient"]),
        new("LinearGradient", ["X1", "Y1", "X2", "Y2"], ["Stop"]),
        new("Stop", ["Offset", "Color"], []),
    ];

    /// <summary>
    /// Gets all elements supported by a complete SlideML document.
    /// </summary>
    public static IReadOnlyList<SlideMlElementSchema> Elements => ElementsValue;

    /// <summary>
    /// Gets the schema for a named element.
    /// </summary>
    public static SlideMlElementSchema? FindElement(string elementName)
    {
        if (string.IsNullOrWhiteSpace(elementName))
        {
            throw new ArgumentException("Element name cannot be empty.", nameof(elementName));
        }

        return ElementsValue.FirstOrDefault(element => string.Equals(element.Name, elementName, StringComparison.Ordinal));
    }
}

/// <summary>
/// Describes one element in a complete SlideML document.
/// </summary>
/// <param name="Name">The case-sensitive element name.</param>
/// <param name="AllowedAttributes">The attributes accepted by the element.</param>
/// <param name="AllowedChildren">The direct child elements accepted by the element.</param>
public sealed record SlideMlElementSchema(
    string Name,
    IReadOnlyList<string> AllowedAttributes,
    IReadOnlyList<string> AllowedChildren);
