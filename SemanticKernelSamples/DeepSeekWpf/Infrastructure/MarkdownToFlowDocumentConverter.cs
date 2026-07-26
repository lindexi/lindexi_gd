using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace DeepSeekWpf.Infrastructure;

public sealed class MarkdownToFlowDocumentConverter : IValueConverter
{
    private static readonly Regex HeadingPattern = new("^(#{1,6})\\s+(.+)$", RegexOptions.Compiled);
    private static readonly Regex ListPattern = new("^\\s*((?:[-*+])|(?:\\d+\\.))\\s+(.+)$", RegexOptions.Compiled);
    private static readonly Regex InlinePattern = new("`([^`]+)`|\\*\\*([^*]+)\\*\\*", RegexOptions.Compiled);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var document = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 14,
            PagePadding = new Thickness(0),
            TextAlignment = TextAlignment.Left,
        };

        var markdown = value as string ?? string.Empty;
        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var paragraphLines = new List<string>();
        var codeLines = new List<string>();
        var inCodeBlock = false;

        void FlushParagraph()
        {
            if (paragraphLines.Count == 0)
            {
                return;
            }

            var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, 8) };
            AppendInlines(paragraph.Inlines, string.Join("\n", paragraphLines));
            document.Blocks.Add(paragraph);
            paragraphLines.Clear();
        }

        void FlushCodeBlock()
        {
            var paragraph = new Paragraph
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                Margin = new Thickness(0, 4, 0, 10),
                Padding = new Thickness(12),
                Background = new SolidColorBrush(Color.FromRgb(243, 244, 246)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
                BorderThickness = new Thickness(1),
            };
            paragraph.Inlines.Add(new Run(string.Join("\n", codeLines)));
            document.Blocks.Add(paragraph);
            codeLines.Clear();
        }

        foreach (var rawLine in lines)
        {
            if (rawLine.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                if (inCodeBlock)
                {
                    FlushCodeBlock();
                    inCodeBlock = false;
                }
                else
                {
                    FlushParagraph();
                    inCodeBlock = true;
                }

                continue;
            }

            if (inCodeBlock)
            {
                codeLines.Add(rawLine);
                continue;
            }

            if (string.IsNullOrWhiteSpace(rawLine))
            {
                FlushParagraph();
                continue;
            }

            var heading = HeadingPattern.Match(rawLine);
            if (heading.Success)
            {
                FlushParagraph();
                var level = heading.Groups[1].Value.Length;
                var paragraph = new Paragraph
                {
                    FontWeight = FontWeights.SemiBold,
                    FontSize = Math.Max(15, 24 - (level * 2)),
                    Margin = new Thickness(0, 6, 0, 8),
                };
                AppendInlines(paragraph.Inlines, heading.Groups[2].Value);
                document.Blocks.Add(paragraph);
                continue;
            }

            var listItem = ListPattern.Match(rawLine);
            if (listItem.Success)
            {
                FlushParagraph();
                var paragraph = new Paragraph { Margin = new Thickness(18, 0, 0, 6) };
                paragraph.Inlines.Add(new Run(listItem.Groups[1].Value.EndsWith(".", StringComparison.Ordinal) ? $"{listItem.Groups[1].Value} " : "• "));
                AppendInlines(paragraph.Inlines, listItem.Groups[2].Value);
                document.Blocks.Add(paragraph);
                continue;
            }

            if (rawLine.StartsWith('>'))
            {
                FlushParagraph();
                var paragraph = new Paragraph
                {
                    Margin = new Thickness(12, 2, 0, 8),
                    Padding = new Thickness(10, 4, 0, 4),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                    BorderThickness = new Thickness(3, 0, 0, 0),
                };
                AppendInlines(paragraph.Inlines, rawLine.TrimStart('>', ' '));
                document.Blocks.Add(paragraph);
                continue;
            }

            paragraphLines.Add(rawLine);
        }

        FlushParagraph();
        if (inCodeBlock || codeLines.Count > 0)
        {
            FlushCodeBlock();
        }

        return document;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;

    private static void AppendInlines(InlineCollection inlines, string text)
    {
        var index = 0;
        foreach (Match match in InlinePattern.Matches(text))
        {
            if (match.Index > index)
            {
                inlines.Add(new Run(text[index..match.Index]));
            }

            if (match.Groups[1].Success)
            {
                inlines.Add(new Run(match.Groups[1].Value)
                {
                    FontFamily = new FontFamily("Consolas"),
                    Background = new SolidColorBrush(Color.FromRgb(243, 244, 246)),
                });
            }
            else
            {
                inlines.Add(new Bold(new Run(match.Groups[2].Value)));
            }

            index = match.Index + match.Length;
        }

        if (index < text.Length)
        {
            inlines.Add(new Run(text[index..]));
        }
    }
}
