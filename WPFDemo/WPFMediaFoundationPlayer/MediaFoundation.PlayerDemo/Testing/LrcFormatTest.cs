using System.Globalization;
using MediaFoundation.Core;

namespace MediaFoundation.PlayerDemo;

internal static class LrcFormatTest
{
    internal static void Run(Action<bool, string> check)
    {
        ArgumentNullException.ThrowIfNull(check);
        var document = Parse("\uFEFF[ar:Artist]\r\n[ti:Title]\r\n[al:Album]\r\n[by:Editor]\r\n[re:Tool][ve:1]\r\n[length:03:00]\r\n[00:03.125]Later\r\n[00:01.25][00:02]First\r\n[00:01.250]Second\r\n[00:04]   \r\n[00:05]Last\r\n");
        check(document.Cues.Count == 4, "LrcCueCount");
        check(document.Cues[0] == new SubtitleCue(TimeSpan.FromMilliseconds(1250), TimeSpan.FromSeconds(2), "First\nSecond"), "LrcStableMergeAndHundredths");
        check(document.Cues[1] == new SubtitleCue(TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(3125), "First"), "LrcMultipleTimestampsAndMilliseconds");
        check(document.Cues[2] == new SubtitleCue(TimeSpan.FromMilliseconds(3125), TimeSpan.FromSeconds(4), "Later"), "LrcClearEndsPreviousCue");
        check(document.Cues[3] == new SubtitleCue(TimeSpan.FromSeconds(5), TimeSpan.MaxValue, "Last"), "LrcLastCueEnd");
        check(document.GetText(TimeSpan.FromMilliseconds(1249)) == "", "LrcBeforeStart");
        check(document.GetText(TimeSpan.FromMilliseconds(1250)) == "First\nSecond", "LrcInclusiveStart");
        check(document.GetText(TimeSpan.FromSeconds(2)) == "First", "LrcExclusiveEnd");
        check(document.GetText(TimeSpan.FromSeconds(4)) == "", "LrcEmptyTimestampClears");
        check(document.GetText(TimeSpan.MaxValue - TimeSpan.FromTicks(1)) == "Last", "LrcLastTextPersists");
        check(document.GetText(TimeSpan.MaxValue) == "", "LrcMaxValueExclusive");
        check(Parse("[00:01]A\n[offset:+250]").Cues[0].Start == TimeSpan.FromMilliseconds(750), "LrcPositiveOffsetAdvancesGlobally");
        check(Parse("[offset:-250]\n[00:01]A").Cues[0].Start == TimeSpan.FromMilliseconds(1250), "LrcNegativeOffsetDelays");
        check(Parse("[offset:500]\n[00:00.100]A\n[00:00.200]B").Cues[0] == new SubtitleCue(TimeSpan.Zero, TimeSpan.MaxValue, "A\nB"), "LrcClampedTimesMergeStably");
        check(Parse("[offset:500]\n[offset:100]\n[00:01]A").Cues[0].Start == TimeSpan.FromMilliseconds(900), "LrcLastOffsetWins");
        check(Parse("[00:02]A\n[00:01]B\n[00:02]C").GetText(TimeSpan.FromSeconds(2)) == "A\nC", "LrcStableOrderAfterSorting");
        check(Parse("[00:01]\n[00:01]A\n[00:01] ").GetText(TimeSpan.FromSeconds(1)) == "A", "LrcTextWinsAtSharedClearTime");
        check(Parse("[00:01]A\n[00:02]").Cues[0].End == TimeSpan.FromSeconds(2), "LrcFinalClearBoundsText");
        check(Parse("[123:45.67]Long").Cues[0].Start == TimeSpan.FromMilliseconds(7425670), "LrcMinutesBeyond59");
        check(Parse("[00:00]A &amp; <b>B</b>").Cues[0].Text == "A &amp; <b>B</b>", "LrcPreservesLiteralText");
        check(Parse("[15372286728:05.477]Edge").Cues[0].Start == TimeSpan.FromTicks(9223372036854770000), "LrcLargestMillisecondTimestamp");
        check(Parse("[offset:922337203685477]\n[00:00]A").Cues[0].Start == TimeSpan.Zero, "LrcLargePositiveOffsetClamps");

        const string timing = "Invalid timestamp or metadata; expected [minutes:ss], [minutes:ss.ff], or [minutes:ss.fff].";
        const string offset = "Invalid offset; expected signed integer milliseconds within the TimeSpan range.";
        const string empty = "No timed lyric text was found.";
        Reject("[ti:Title]\n[00:60]A", 2, timing, check, "LrcRejectSeconds60");
        Reject("[00:01.1]A", 1, timing, check, "LrcRejectTenths");
        Reject("[00:01.1234]A", 1, timing, check, "LrcRejectExcessPrecision");
        Reject("[-1:00]A", 1, timing, check, "LrcRejectNegativeMinutes");
        Reject("[00:1]A", 1, timing, check, "LrcRejectShortSeconds");
        Reject("[00:01,25]A", 1, timing, check, "LrcRejectComma");
        Reject("[00:01A", 1, timing, check, "LrcRejectMissingBracket");
        Reject("[15372286728:05.478]A", 1, timing, check, "LrcRejectTimeSpanOverflow");
        Reject("[9223372036854775808:00]A", 1, timing, check, "LrcRejectMinuteIntegerOverflow");
        Reject("[9223372036854775807:00]A", 1, timing, check, "LrcRejectHugeMinuteArithmeticOverflow");
        Reject("[offset:1.5]\n[00:01]A", 1, offset, check, "LrcRejectFractionalOffset");
        Reject("[offset:]\n[00:01]A", 1, offset, check, "LrcRejectMissingOffset");
        Reject("[offset:9223372036854775808]\n[00:01]A", 1, offset, check, "LrcRejectOffsetIntegerOverflow");
        Reject("[offset:922337203685478]\n[00:01]A", 1, offset, check, "LrcRejectOffsetTickOverflow");
        Reject("[offset:-922337203685478]\n[00:01]A", 1, offset, check, "LrcRejectNegativeOffsetTickOverflow");
        Reject("[offset:-922337203685477]\n[00:00.001]A", 2, "The offset-adjusted timestamp must be less than TimeSpan.MaxValue.", check, "LrcRejectAdjustedOverflow");
        Reject("[ar:Artist]\n[00:01]<00:01.00>Word", 2, "Enhanced LRC word timestamps are not supported.", check, "LrcRejectEnhancedWithLineAndReason");
        Reject("[00:01]Word<00:02.123>Next", 1, "Enhanced LRC word timestamps are not supported.", check, "LrcRejectEmbeddedWordTiming");
        Reject("", 1, empty, check, "LrcRejectEmpty");
        Reject("[ar:Artist]\n[ti:Title]", 1, empty, check, "LrcRejectMetadataOnly");
        Reject("[00:01]\n[00:02]   ", 1, empty, check, "LrcRejectClearOnly");
        Reject("Untimed text", 1, timing, check, "LrcRejectUntimedText");
    }

    private static SubtitleDocument Parse(string text) => SubtitleReader.Parse(text, SubtitleFormat.Lrc);

    private static void Reject(string text, int line, string reason, Action<bool, string> check, string name)
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        bool rejected = false;
        try
        {
            // Keep exact diagnostic assertions independent of the machine's UI language.
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            Parse(text);
        }
        catch (FormatException exception)
        {
            rejected = exception.Message == string.Format(CultureInfo.CurrentCulture, "LRC line {0}: {1}", line, reason);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
        check(rejected, name);
    }
}
