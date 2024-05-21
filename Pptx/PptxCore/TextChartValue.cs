namespace PptxCore;

public record TextChartValue(string? StringValueText) : IChartValue
{
    public string? GetViewText()
    {
        return StringValueText;
    }
}