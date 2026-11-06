using System.Globalization;
using MediaFoundation.Core;

namespace MediaFoundation.PlayerDemo;

internal static class SubViewerFormatTest
{
    private const string Header = "[INFORMATION]\n[END INFORMATION]\n[SUBTITLE]\n";
    private const string Cue = "00:00:01.23,00:00:02.34\nFirst[br]Second";
    private const string TimingReason = "Invalid time range; expected hh:mm:ss.cc,hh:mm:ss.cc with ASCII digits, minutes and seconds below 60, and end greater than start.";
    private const string StyleReason = "Style, control and unknown bracket tags other than [br] cannot be represented by this plain-text timeline.";

    internal static void Run(Action<bool, string> check)
    {
        ArgumentNullException.ThrowIfNull(check);
        var document = Parse("\uFEFF" + (Header + Cue).Replace("\n", "\r\n"));
        check(document.Cues.Count == 1, "SubViewerCueCount");
        check(document.Cues[0].Start == TimeSpan.FromTicks(12300000), "SubViewerCentisecondStart");
        check(document.Cues[0].End == TimeSpan.FromTicks(23400000), "SubViewerCentisecondEnd");
        check(document.Cues[0].Text == "First\nSecond", "SubViewerBracketLineBreak");
        check(document.GetText(TimeSpan.FromMilliseconds(1230)) == "First\nSecond", "SubViewerInclusiveStart");
        check(document.GetText(TimeSpan.FromMilliseconds(2340)) == "", "SubViewerExclusiveEnd");
        check(Parse(Header + "00:00:00.00,00:00:00.01\nA").Cues[0].End == TimeSpan.FromMilliseconds(10), "SubViewerOneCentisecond");
        check(Parse(Header + "99:59:59.98,99:59:59.99\nA").Cues[0].End == TimeSpan.FromMilliseconds(359999990), "SubViewerMaximumTwoDigitHours");
        check(Parse(Header + "00:00:00.00,00:00:02.00\nA[BR]B\nC").Cues[0].Text == "A\nB\nC", "SubViewerPhysicalAndBracketLines");
        check(Parse(Header + "00:00:00.00,00:00:02.00\nA &amp; B <i>C</i>").Cues[0].Text == "A &amp; B <i>C</i>", "SubViewerLiteralPlainText");
        var overlap = Parse(Header + "00:00:01.00,00:00:03.00\nLater\n\n00:00:00.00,00:00:02.00\nEarlier");
        check(overlap.GetText(TimeSpan.FromMilliseconds(1500)) == "Earlier\nLater", "SubViewerSortedOverlap");
        check(overlap.GetText(TimeSpan.FromSeconds(2)) == "Later", "SubViewerOverlapEndBoundary");
        check(Parse("\n[INFORMATION]\n[TITLE]Title\n[AUTHOR]Author\n[SOURCE]Source\n[PRG]Editor\n[FILEPATH]file.sub\n[COMMENT]Comment\n[DATE]2026\n[VERSION]2.0\n[DELAY]0\n[CD TRACK]0\n[END INFORMATION]\n\n[SUBTITLE]\n" + Cue).Cues[0].Text == "First\nSecond", "SubViewerKnownMetadata");
        check(Parse((Header + Cue).Replace('\n', '\r')).Cues.Count == 1, "SubViewerCarriageReturnLines");

        Reject("", 1, "No subtitle cues were found.", check, "SubViewerEmpty");
        Reject(" \n\t", 1, "No subtitle cues were found.", check, "SubViewerWhitespace");
        Reject(Header, 1, "No subtitle cues were found.", check, "SubViewerHeaderOnly");
        Reject("00:00:01.23,00:00:02.34\nA", 1, "Expected [INFORMATION]; only the SubViewer 2.0 sectioned format is supported, not SubViewer 1.0.", check, "SubViewerRejectHeaderless");
        Reject("{T 00:00:01:00}\nA", 1, "Expected [INFORMATION]; only the SubViewer 2.0 sectioned format is supported, not SubViewer 1.0.", check, "SubViewerRejectVersionOneSyntax");
        Reject("[INFORMATION]\n[TITLE]A", 1, "The [INFORMATION] section is missing [END INFORMATION].", check, "SubViewerMissingInformationEnd");
        Reject("[INFORMATION]\n[END INFORMATION]\n[OTHER]", 3, "Expected [SUBTITLE] after [END INFORMATION].", check, "SubViewerMissingSubtitleSection");
        Reject("[INFORMATION]\n[VERSION]1.0\n[END INFORMATION]\n[SUBTITLE]\n" + Cue, 2, "Only SubViewer version 2.0 is supported.", check, "SubViewerRejectVersionOneDeclaration");
        Reject("[INFORMATION]\n[VERSION]3.0", 2, "Only SubViewer version 2.0 is supported.", check, "SubViewerRejectUnknownVersion");
        Reject("[INFORMATION]\n[TITLE]A\n[TITLE]B", 3, "Invalid, duplicate or unsupported information tag.", check, "SubViewerDuplicateMetadata");
        Reject("[INFORMATION]\n[UNKNOWN]A", 2, "Invalid, duplicate or unsupported information tag.", check, "SubViewerUnknownMetadata");
        Reject("[INFORMATION]\nTITLE A", 2, "Invalid, duplicate or unsupported information tag.", check, "SubViewerMalformedMetadata");
        Reject("[INFORMATION]\n[DELAY]10", 2, "DELAY and CD TRACK are supported only with the value 0; other values cannot be represented by this plain-text timeline.", check, "SubViewerRejectDelay");
        Reject("[INFORMATION]\n[CD TRACK]1", 2, "DELAY and CD TRACK are supported only with the value 0; other values cannot be represented by this plain-text timeline.", check, "SubViewerRejectTrack");
        Reject(Header + "[COLF]&HFFFFFF,[STYLE]bd,[SIZE]24,[FONT]Arial\n" + Cue, 4, StyleReason, check, "SubViewerRejectCombinedStyleHeader");
        Reject(Header + "[FONT]Arial\n" + Cue, 4, StyleReason, check, "SubViewerRejectFont");
        Reject(Header + "[SIZE]24\n" + Cue, 4, StyleReason, check, "SubViewerRejectSize");
        Reject(Header + "[STYLE]no\n" + Cue, 4, StyleReason, check, "SubViewerRejectStyle");
        Reject("[INFORMATION]\n[COLF]&HFFFFFF", 2, StyleReason, check, "SubViewerRejectStyleInsideMetadata");
        Reject(Header + "00:00:01.00,00:00:02.00\nA[unknown]B", 5, StyleReason, check, "SubViewerRejectInlineControl");
        Reject(Header + "00:60:00.00,01:01:00.00\nA", 4, TimingReason, check, "SubViewerInvalidMinutes");
        Reject(Header + "00:00:60.00,00:01:02.00\nA", 4, TimingReason, check, "SubViewerInvalidSeconds");
        Reject(Header + "00:00:01.00,00:00:01.00\nA", 4, TimingReason, check, "SubViewerEqualTimes");
        Reject(Header + "00:00:02.00,00:00:01.00\nA", 4, TimingReason, check, "SubViewerReversedTimes");
        Reject(Header + "-1:00:00.00,00:00:01.00\nA", 4, TimingReason, check, "SubViewerNegativeTime");
        Reject(Header + "00:00:01.000,00:00:02.000\nA", 4, TimingReason, check, "SubViewerRejectMilliseconds");
        Reject(Header + "0:00:01.00,00:00:02.00\nA", 4, TimingReason, check, "SubViewerRejectShortHours");
        Reject(Header + "100:00:01.00,100:00:02.00\nA", 4, TimingReason, check, "SubViewerRejectLongHours");
        Reject(Header + "00:00:01.00 --> 00:00:02.00\nA", 4, TimingReason, check, "SubViewerRejectOtherTimeSyntax");
        Reject(Header + "00:00:01.00,00:00:02.00,00:00:03.00\nA", 4, TimingReason, check, "SubViewerRejectExtraTime");
        Reject(Header + "00:00:01.00,00:00:02.00 extra\nA", 4, TimingReason, check, "SubViewerRejectTimeSettings");
        Reject(Header + "00:00:01.00,00:00:02.00\n[br]  [br]", 4, "The subtitle text must not be empty.", check, "SubViewerEmptyCue");
        Reject(Header + "00:00:01.00,00:00:02.00", 4, "The subtitle text must not be empty.", check, "SubViewerMissingText");
        Reject(Header + Cue + "\n00:00:03.00,00:00:04.00\nB", 6, "A blank line is required between subtitle cues.", check, "SubViewerMissingCueSeparator");
    }

    private static SubtitleDocument Parse(string text) => SubtitleReader.Parse(text, SubtitleFormat.SubViewer);

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
            rejected = exception.Message == string.Format(CultureInfo.CurrentCulture, "SubViewer 2.0 line {0}: {1}", line, reason);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
        check(rejected, name);
    }
}
