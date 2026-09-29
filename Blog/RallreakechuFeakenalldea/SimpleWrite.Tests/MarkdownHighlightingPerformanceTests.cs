using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using LightTextEditorPlus;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Highlighters;
using Xunit;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SimpleWrite.Tests;

public class MarkdownHighlightingPerformanceTests(ITestOutputHelper output)
{

    public static IEnumerable<object[]> Scenarios()
    {
        foreach (var blocks in new[] { 20, 100 })
        foreach (var content in new[] { "Plain", "Headings", "Lists", "Links", "CSharp", "Json", "Xml", "JavaScript", "Python", "Unknown" })
        foreach (var position in new[] { "Start", "Middle", "End", "Unchanged" })
        foreach (var highlight in new[] { false, true })
            yield return [blocks, content, position, highlight];
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    [Trait("Category", "Performance")]
    public void RepeatedTypingReportsHighlightingCost(int blocks, string content, string position, bool highlight)
    {
        // Large inputs currently take seconds per keystroke; keep the automatic matrix bounded.
        var SampleCount = blocks >= 100 ? 8 : 32;
        // Warm up on a separate instance so measured documents have identical initial states.
        var warmText = CreateDocument(2, content);
        var warmEditor = new TextEditor();
        warmEditor.AppendText(warmText);
        var warmHighlighter = new MarkdownDocumentHighlighter(warmEditor);
        warmHighlighter.ApplyHighlight(warmText);
        for (var i = 0; i < 4; i++)
        {
            warmEditor.TextEditorCore.EditAndReplace("x", new Selection(new CaretOffset(0), 0));
            warmHighlighter.ApplyHighlight(GetText(warmEditor));
        }

        var editor = new TextEditor();
        var initialText = CreateDocument(blocks, content);
        editor.AppendText(initialText);
        var expectedText = GetText(editor);
        var highlighter = new MarkdownDocumentHighlighter(editor);
        if (highlight)
            highlighter.ApplyHighlight(expectedText);

        var insertionIndex = position switch
        {
            "Start" => 0,
            "Middle" => expectedText.IndexOf("Paragraph", expectedText.Length / 2, StringComparison.Ordinal),
            _ => expectedText.Length
        };
        var samples = new List<Sample>(SampleCount);
        for (var i = 0; i < SampleCount; i++)
        {
            var selection = new Selection(new CaretOffset(
                TextIndexConverter.ConvertUtf16IndexToDocumentOffset(expectedText, insertionIndex).Offset), 0);
            var allocatedStart = GC.GetAllocatedBytesForCurrentThread();
            var start = Stopwatch.GetTimestamp();
            if (position != "Unchanged")
                editor.TextEditorCore.EditAndReplace("x", selection);
            var editEnd = Stopwatch.GetTimestamp();
            var allocatedEdit = GC.GetAllocatedBytesForCurrentThread();
            var text = GetText(editor);
            var extractEnd = Stopwatch.GetTimestamp();
            var allocatedExtract = GC.GetAllocatedBytesForCurrentThread();
            if (highlight)
                highlighter.ApplyHighlight(text);
            var end = Stopwatch.GetTimestamp();
            var allocatedEnd = GC.GetAllocatedBytesForCurrentThread();

            samples.Add(new Sample(
                Stopwatch.GetElapsedTime(start, editEnd).TotalMilliseconds,
                Stopwatch.GetElapsedTime(editEnd, extractEnd).TotalMilliseconds,
                Stopwatch.GetElapsedTime(extractEnd, end).TotalMilliseconds,
                Stopwatch.GetElapsedTime(start, end).TotalMilliseconds,
                allocatedEdit - allocatedStart, allocatedExtract - allocatedEdit,
                allocatedEnd - allocatedExtract, allocatedEnd - allocatedStart));
            if (position != "Unchanged")
            {
                expectedText = expectedText.Insert(insertionIndex, "x");
                insertionIndex++;
            }
        }

        var report = new
        {
            Runtime = RuntimeInformation.FrameworkDescription,
            OS = RuntimeInformation.OSDescription,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            Configuration = BuildConfiguration,
            DebuggerAttached = Debugger.IsAttached,
            ProcessorCount = Environment.ProcessorCount,
            Blocks = blocks,
            Content = content,
            Position = position,
            Highlight = highlight,
            InitialUtf16Length = initialText.Length,
            SampleCount,
            Edit = Summarize(samples.Select(s => s.EditMilliseconds)),
            Extract = Summarize(samples.Select(s => s.ExtractMilliseconds)),
            Highlighting = Summarize(samples.Select(s => s.HighlightMilliseconds)),
            Total = Summarize(samples.Select(s => s.TotalMilliseconds)),
            MeanAllocatedBytes = samples.Average(s => s.TotalAllocatedBytes),
            MeanHighlightAllocatedBytes = samples.Average(s => s.HighlightAllocatedBytes),
            Samples = samples
        };
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        var directory = Path.Combine(AppContext.BaseDirectory, "PerformanceResults");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{content}-{blocks}-{position}-{highlight}-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        output.WriteLine(path);
        output.WriteLine(json);

        Assert.Equal(expectedText, GetText(editor));
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void InsertingBeforeHeadingPreservesHeadingStyle(string newline)
    {
        var editor = new TextEditor();
        editor.AppendText($"Paragraph \U0001F600{newline}{newline}# Heading{newline}");
        var highlighter = new MarkdownDocumentHighlighter(editor);
        highlighter.ApplyHighlight(GetText(editor));
        editor.TextEditorCore.EditAndReplace("x", new Selection(new CaretOffset(0), 0));
        var text = GetText(editor);
        highlighter.ApplyHighlight(text);
        var offset = TextIndexConverter.ConvertUtf16IndexToDocumentOffset(
            text, text.IndexOf("Heading", StringComparison.Ordinal)).Offset;
        var properties = editor.GetRunPropertyRange(new Selection(new CaretOffset(offset), 1));

        Assert.All(properties, property => Assert.Equal(editor.StyleRunProperty.FontSize + 10, property.FontSize));
    }

    [Theory]
    [InlineData("\n", false)]
    [InlineData("\r\n", false)]
    [InlineData("\n", true)]
    [InlineData("\r\n", true)]
    public void LinkEditsPreserveNormalTextStyle(string newline, bool removeLink)
    {
        var editor = new TextEditor();
        editor.AppendText($"Paragraph \U0001F600{newline}{newline}https://example.com tail");
        var highlighter = new MarkdownDocumentHighlighter(editor);
        highlighter.ApplyHighlight(GetText(editor));
        var text = GetText(editor);
        var index = text.IndexOf("https://", StringComparison.Ordinal);
        var offset = TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, index).Offset;
        var selection = new Selection(new CaretOffset(offset), removeLink ? "https://example.com".Length : 0);
        editor.TextEditorCore.EditAndReplace(removeLink ? "ordinary" : "x ", selection);
        text = GetText(editor);
        highlighter.ApplyHighlight(text);
        var token = removeLink ? "ordinary" : "tail";
        offset = TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text,
            text.IndexOf(token, StringComparison.Ordinal)).Offset;
        var properties = editor.GetRunPropertyRange(new Selection(new CaretOffset(offset), token.Length)).ToArray();

        Assert.Equal(Enumerable.Repeat(editor.StyleRunProperty, properties.Length), properties);
    }

    private static string GetText(TextEditor editor)
    {
        var selection = editor.GetAllDocumentSelection();
        return editor.TextEditorCore.GetText(in selection);
    }

    private static string CreateDocument(int blocks, string content)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < blocks; i++)
        {
            builder.Append("Paragraph ").Append(i).Append(" with ordinary text.\n\n");
            builder.Append(content switch
            {
                "Plain" => "Paragraph with more ordinary text.\n\n",
                "Headings" => "# Heading\n\n## Second heading\n\n### Third heading\n\n",
                "Lists" => "- First item\n- Second item\n  - Nested item\n\n> Quoted text\n\n",
                "Links" => "# Heading\n\nParagraph with https://example.com/path and more text.\n\n",
                "CSharp" => "# Heading\n\n```csharp\npublic class Example { public int Value => 42; }\n```\n\n",
                "Json" => "```json\n{\"name\": \"example\", \"value\": 42, \"enabled\": true}\n```\n\n",
                "Xml" => "```xml\n<item name=\"example\"><value>42</value></item>\n```\n\n",
                "JavaScript" => "```javascript\nconst value = 42; // example\nconsole.log(value);\n```\n\n",
                "Python" => "```python\ndef example(value):\n    return value + 42 # result\n```\n\n",
                "Unknown" => "```unknown-language\nvalue = 42; sample text\n```\n\n",
                _ => throw new ArgumentOutOfRangeException(nameof(content), content, null)
            });
        }
        return builder.ToString();
    }

    private static Summary Summarize(IEnumerable<double> values)
    {
        var sorted = values.Order().ToArray();
        return new Summary((sorted[(sorted.Length - 1) / 2] + sorted[sorted.Length / 2]) / 2,
            sorted[(int) Math.Ceiling(sorted.Length * 0.95) - 1], sorted[^1]);
    }

#if DEBUG
    private const string BuildConfiguration = "Debug";
#else
    private const string BuildConfiguration = "Release";
#endif

    private sealed record Summary(double MedianMilliseconds, double P95Milliseconds, double MaxMilliseconds);

    private sealed record Sample(double EditMilliseconds, double ExtractMilliseconds,
        double HighlightMilliseconds, double TotalMilliseconds, long EditAllocatedBytes,
        long ExtractAllocatedBytes, long HighlightAllocatedBytes, long TotalAllocatedBytes);
}
