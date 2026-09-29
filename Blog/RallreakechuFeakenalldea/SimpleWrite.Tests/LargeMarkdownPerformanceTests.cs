using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using LightTextEditorPlus;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Highlighters;
using Xunit;
using Xunit.Abstractions;

namespace SimpleWrite.Tests;

public class LargeMarkdownPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", "Performance")]
    public void OpenThousandLinesAndTypeAtMiddleParagraphReportsCost()
    {
        const int lineCount = 1000;
        const int lineLength = 200;
        var lines = Enumerable.Range(0, lineCount).Select(index =>
        {
            var prefix = index % 20 == 19
                ? $"## 第{index + 1}行标题 "
                : $"第{index + 1}行正文包含**加粗文字**、*强调文字*和[参考链接](https://example.com)。";
            return prefix.PadRight(lineLength, '文');
        }).ToArray();
        var document = string.Join("\n", lines);
        var openSamples = new List<OpenSample>();
        var typingSamples = new List<TypingSample>();
        string? finalText = null;
        string? expectedFinalText = null;

        // The first open includes cold initialization; subsequent opens use fresh editors in the same process.
        for (var iteration = 0; iteration < 3; iteration++)
        {
            var allocatedStart = GC.GetAllocatedBytesForCurrentThread();
            var start = Stopwatch.GetTimestamp();
            var editor = new TextEditor();
            var highlighter = new MarkdownDocumentHighlighter(editor);
            var created = Stopwatch.GetTimestamp();
            editor.AppendText(document);
            var loaded = Stopwatch.GetTimestamp();
            var text = GetText(editor);
            var extracted = Stopwatch.GetTimestamp();
            highlighter.ApplyHighlight(text);
            var highlighted = Stopwatch.GetTimestamp();
            var allocatedEnd = GC.GetAllocatedBytesForCurrentThread();
            openSamples.Add(new OpenSample(iteration,
                Elapsed(start, created), Elapsed(created, loaded), Elapsed(loaded, extracted),
                Elapsed(extracted, highlighted), Elapsed(start, highlighted), allocatedEnd - allocatedStart));

            // Use the third freshly opened document for a fixed sequence of real character insertions.
            if (iteration != 2)
                continue;

            var expectedText = text;
            var insertionIndex = text.IndexOf("第501行正文", StringComparison.Ordinal);
            Assert.True(insertionIndex >= 0);
            foreach (var character in "这里开始连续输入测试文字")
            {
                var input = character.ToString();
                var selection = new Selection(new CaretOffset(
                    TextIndexConverter.ConvertUtf16IndexToDocumentOffset(expectedText, insertionIndex).Offset), 0);
                allocatedStart = GC.GetAllocatedBytesForCurrentThread();
                start = Stopwatch.GetTimestamp();
                editor.TextEditorCore.EditAndReplace(input, selection);
                var edited = Stopwatch.GetTimestamp();
                var allocatedEdit = GC.GetAllocatedBytesForCurrentThread();
                text = GetText(editor);
                extracted = Stopwatch.GetTimestamp();
                var allocatedExtract = GC.GetAllocatedBytesForCurrentThread();
                highlighter.ApplyHighlight(text);
                highlighted = Stopwatch.GetTimestamp();
                allocatedEnd = GC.GetAllocatedBytesForCurrentThread();
                typingSamples.Add(new TypingSample(Elapsed(start, edited), Elapsed(edited, extracted),
                    Elapsed(extracted, highlighted), Elapsed(start, highlighted),
                    allocatedEdit - allocatedStart, allocatedExtract - allocatedEdit,
                    allocatedEnd - allocatedExtract, allocatedEnd - allocatedStart));
                expectedText = expectedText.Insert(insertionIndex, input);
                insertionIndex += input.Length;
            }
            expectedFinalText = expectedText;
            finalText = GetText(editor);
        }

        var report = new
        {
            Runtime = RuntimeInformation.FrameworkDescription,
            OS = RuntimeInformation.OSDescription,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
#if DEBUG
            Configuration = "Debug",
#else
            Configuration = "Release",
#endif
            DebuggerAttached = Debugger.IsAttached,
            LineCount = lineCount,
            CharactersPerLine = lineLength,
            InitialUtf16Length = document.Length,
            InsertionLine = 501,
            OpenSamples = openSamples,
            TypingSampleCount = typingSamples.Count,
            TypingEdit = Summarize(typingSamples.Select(s => s.EditMilliseconds)),
            TypingExtract = Summarize(typingSamples.Select(s => s.ExtractMilliseconds)),
            TypingHighlight = Summarize(typingSamples.Select(s => s.HighlightMilliseconds)),
            TypingTotal = Summarize(typingSamples.Select(s => s.TotalMilliseconds)),
            MeanTypingAllocatedBytes = typingSamples.Average(s => s.TotalAllocatedBytes),
            MeanTypingHighlightAllocatedBytes = typingSamples.Average(s => s.HighlightAllocatedBytes),
            TypingSamples = typingSamples
        };
        var directory = Path.Combine(AppContext.BaseDirectory, "PerformanceResults");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"Markdown-1000Lines-200Chars-{Guid.NewGuid():N}.json");
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
        output.WriteLine(path);
        output.WriteLine(json);

        Assert.Equal(expectedFinalText, finalText);
    }

    private static string GetText(TextEditor editor)
    {
        var selection = editor.GetAllDocumentSelection();
        return editor.TextEditorCore.GetText(in selection);
    }

    private static double Elapsed(long start, long end) => Stopwatch.GetElapsedTime(start, end).TotalMilliseconds;

    private static Summary Summarize(IEnumerable<double> values)
    {
        var sorted = values.Order().ToArray();
        return new Summary((sorted[(sorted.Length - 1) / 2] + sorted[sorted.Length / 2]) / 2,
            sorted[(int)Math.Ceiling(sorted.Length * 0.95) - 1], sorted[^1]);
    }

    private sealed record Summary(double MedianMilliseconds, double P95Milliseconds, double MaxMilliseconds);
    private sealed record OpenSample(int Iteration, double CreateMilliseconds, double LoadMilliseconds,
        double ExtractMilliseconds, double HighlightMilliseconds, double TotalMilliseconds, long AllocatedBytes);
    private sealed record TypingSample(double EditMilliseconds, double ExtractMilliseconds,
        double HighlightMilliseconds, double TotalMilliseconds, long EditAllocatedBytes,
        long ExtractAllocatedBytes, long HighlightAllocatedBytes, long TotalAllocatedBytes);
}
