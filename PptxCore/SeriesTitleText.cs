using DocumentFormat.OpenXml.Drawing.Charts;

namespace PptxCore;

/// <summary>
///     系列标题内容
/// </summary>
public class SeriesTitleText
{
    public SeriesTitleText(string text)
    {
        Text = text;
    }

    public string Text { get; }

    public override string ToString()
    {
        return $"SeriesTitleText: {Text}";
    }

    public static SeriesTitleText ToSeriesTitleText(SeriesText? seriesText, AreaChartRenderContext context)
    {
        var text = string.Empty;
        if (seriesText != null)
        {
            var chartValueList = ChartValueList.GetChartValueList(seriesText, context);
            var chartValue = chartValueList.ValueList.FirstOrDefault();
            if (chartValue != null)
            {
                text = chartValue.GetViewText();
            }

            if (string.IsNullOrEmpty(text))
            {
                text = seriesText.NumericValue?.Text;
            }
        }

        return new SeriesTitleText(text ?? string.Empty);
    }
}