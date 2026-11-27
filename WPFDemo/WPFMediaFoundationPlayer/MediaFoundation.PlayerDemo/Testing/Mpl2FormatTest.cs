using System.Globalization;
using MediaFoundation.Core;

namespace MediaFoundation.PlayerDemo;

internal static class Mpl2FormatTest
{
    private const string TimingReason = "Invalid time range; expected [start][end] with nonnegative ASCII integer deciseconds and end greater than start.";
    private const string RangeReason = "The decisecond timestamp exceeds TimeSpan limits.";
    private const string ItalicReason = "A leading slash on an MPL2 text line denotes italics and is not supported by this plain-text parser.";

    internal static void Run(Action<bool, string> check)
    {
        ArgumentNullException.ThrowIfNull(check);
        var document = Parse("\uFEFF\r\n[12][34]First|Second\r\n\r\n");
        check(document.Cues.Count == 1, "Mpl2CueCount");
        check(document.Cues[0].Start == TimeSpan.FromTicks(12000000), "Mpl2DecisecondStart");
        check(document.Cues[0].End == TimeSpan.FromTicks(34000000), "Mpl2DecisecondEnd");
        check(document.Cues[0].Text == "First\nSecond", "Mpl2PipeLineBreak");
        check(document.GetText(TimeSpan.FromTicks(11999999)) == "", "Mpl2BeforeStart");
        check(document.GetText(TimeSpan.FromTicks(12000000)) == "First\nSecond", "Mpl2InclusiveStart");
        check(document.GetText(TimeSpan.FromTicks(33999999)) == "First\nSecond", "Mpl2BeforeEnd");
        check(document.GetText(TimeSpan.FromTicks(34000000)) == "", "Mpl2ExclusiveEnd");
        check(Parse("[0][1]A").Cues[0].End == TimeSpan.FromTicks(1000000), "Mpl2OneDecisecond");
        check(Parse("[000][001]A").Cues[0].Start == TimeSpan.Zero, "Mpl2LeadingZeros");
        check(Parse("[9223372036853][9223372036854]A").Cues[0].End == TimeSpan.FromTicks(9223372036854000000L), "Mpl2MaximumDeciseconds");
        check(Parse("[0][1]A\r[1][2]B").Cues.Count == 2, "Mpl2CarriageReturnLines");
        check(Parse("  [0][1]  A  ").Cues[0].Text == "  A  ", "Mpl2TextWhitespacePreserved");
        check(Parse("[0][1]A||B|").Cues[0].Text == "A\n\nB\n", "Mpl2EmptyTextLinesPreserved");
        check(Parse("[0][1]A/B https://example.test &amp; <i>C</i> [note] {text}").Cues[0].Text == "A/B https://example.test &amp; <i>C</i> [note] {text}", "Mpl2LiteralText");
        var overlap = Parse("[10][30]Later\n[0][20]Earlier\n[10][15]Same start");
        check(overlap.Cues[0].Text == "Earlier", "Mpl2SortByStart");
        check(overlap.GetText(TimeSpan.FromSeconds(1)) == "Earlier\nLater\nSame start", "Mpl2StableOverlap");
        check(overlap.GetText(TimeSpan.FromSeconds(2)) == "Later", "Mpl2OverlapEndBoundary");
        check(overlap.GetText(TimeSpan.FromSeconds(4)) == "", "Mpl2SeekBeyondEnd");
        check(overlap.GetText(TimeSpan.FromMilliseconds(500)) == "Earlier", "Mpl2BackwardSeek");
        check(overlap.GetText(TimeSpan.FromSeconds(-1)) == "", "Mpl2NegativePosition");
        check(overlap.GetText(TimeSpan.FromSeconds(1)) == "Earlier\nLater\nSame start", "Mpl2RepeatSeek");

        Reject("", 1, "No subtitle cues were found.", check, "Mpl2Empty");
        Reject("\uFEFF \r\n\t", 1, "No subtitle cues were found.", check, "Mpl2Whitespace");
        Reject("[0][1]", 1, "The subtitle text must not be empty.", check, "Mpl2MissingText");
        Reject("[0][1] | \t|", 1, "The subtitle text must not be empty.", check, "Mpl2BlankMultiline");
        Reject("[-1][2]A", 1, TimingReason, check, "Mpl2NegativeStart");
        Reject("[0][-2]A", 1, TimingReason, check, "Mpl2NegativeEnd");
        Reject("[2][2]A", 1, TimingReason, check, "Mpl2EqualTimes");
        Reject("[3][2]A", 1, TimingReason, check, "Mpl2ReversedTimes");
        Reject("[+0][2]A", 1, TimingReason, check, "Mpl2SignedTime");
        Reject("[0.5][2]A", 1, TimingReason, check, "Mpl2FractionalTime");
        Reject("[0][1e2]A", 1, TimingReason, check, "Mpl2ExponentTime");
        Reject("[ 0][2]A", 1, TimingReason, check, "Mpl2WhitespaceTime");
        Reject("[\uFF10][2]A", 1, TimingReason, check, "Mpl2NonAsciiTime");
        Reject("[][2]A", 1, TimingReason, check, "Mpl2EmptyTime");
        Reject("[0[2]A", 1, TimingReason, check, "Mpl2MissingBracket");
        Reject("[0]A", 1, TimingReason, check, "Mpl2MissingEnd");
        Reject("[0] [2]A", 1, TimingReason, check, "Mpl2SeparatedTimestamps");
        Reject("{0}{2}A", 1, TimingReason, check, "Mpl2OtherFormat");
        Reject("[9223372036855][9223372036856]A", 1, RangeReason, check, "Mpl2StartOverflow");
        Reject("[0][9223372036855]A", 1, RangeReason, check, "Mpl2EndOverflow");
        Reject("[0][9223372036854775808]A", 1, RangeReason, check, "Mpl2Int64Overflow");
        Reject("[0][999999999999999999999999999999999]A", 1, RangeReason, check, "Mpl2HugeTimestamp");
        Reject("[0][1]/Italic", 1, ItalicReason, check, "Mpl2LeadingItalic");
        Reject("[0][1]Plain|/Italic", 1, ItalicReason, check, "Mpl2SecondLineItalic");
        Reject("[0][1]Plain| \t/Italic", 1, ItalicReason, check, "Mpl2IndentedItalic");
        Reject("[0][1]/", 1, ItalicReason, check, "Mpl2ItalicOnly");
        Reject("\uFEFF\r\n[0][1]A\r\n\r\n[2][1]B", 4, TimingReason, check, "Mpl2PhysicalErrorLine");
        Reject("[0][1]A\nUnframed text", 2, TimingReason, check, "Mpl2RejectPhysicalContinuation");

        bool nullRejected = false;
        try
        {
            Parse(null!);
        }
        catch (ArgumentNullException exception)
        {
            nullRejected = exception.ParamName == "text";
        }
        check(nullRejected, "Mpl2NullInput");
    }

    private static SubtitleDocument Parse(string text) => SubtitleReader.Parse(text, SubtitleFormat.Mpl2);

    private static void Reject(string text, int line, string reason, Action<bool, string> check, string name)
    {
        string originalLanguage = SR.Current.IetfLanguageTag;
        bool rejected = false;
        try
        {
            // Keep exact diagnostic assertions independent of the machine's UI language.
            SR.SetCurrent("en");
            Parse(text);
        }
        catch (FormatException exception)
        {
            rejected = exception.Message == string.Format(CultureInfo.CurrentCulture, "MPL2 line {0}: {1}", line, reason);
        }
        finally
        {
            SR.SetCurrent(originalLanguage);
        }
        check(rejected, name);
    }
}
