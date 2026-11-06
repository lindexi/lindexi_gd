using System.Globalization;
using MediaFoundation.Core;

namespace MediaFoundation.PlayerDemo;

internal static class MicroDvdFormatTest
{
    internal static void Run(Action<bool, string> check)
    {
        ArgumentNullException.ThrowIfNull(check);
        var document = Parse("\uFEFF{1}{1}25\r\n{25}{50}First|Second\r\n{75}{100}Later");
        check(document.Cues.Count == 2, "MicroDvdCueCount");
        check(document.Cues[0] == new SubtitleCue(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), "First\nSecond"), "MicroDvdTimingAndLineBreak");
        check(document.GetText(TimeSpan.FromSeconds(1)) == "First\nSecond", "MicroDvdInclusiveStart");
        check(document.GetText(TimeSpan.FromSeconds(2)) == "", "MicroDvdExclusiveEnd");
        check(document.Cues[1] == new SubtitleCue(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(4), "Later"), "MicroDvdLaterCue");
        check(Parse("{0}{0}24\n{12}{24}Half").Cues[0].Start == TimeSpan.FromMilliseconds(500), "MicroDvdZeroDeclaration");
        check(Parse("{30}{60}Explicit", 30).Cues[0].End == TimeSpan.FromSeconds(2), "MicroDvdExplicitFrameRate");
        check(Parse("{1}{1}25\n{0}{25}Match", 25).Cues[0].Start == TimeSpan.Zero, "MicroDvdMatchingFrameRates");
        check(Parse("\n\n{0}{0}2.5\n{1}{5}Fractional").Cues[0].Start == TimeSpan.FromMilliseconds(400), "MicroDvdFractionalRateAndLeadingBlankLines");
        check(Parse("{1}{2}Rounded", 3).Cues[0].Start == TimeSpan.FromTicks(3333333), "MicroDvdTickRounding");
        check(Parse("{25}{50}Later\n{0}{25}Earlier", 25).Cues[0].Text == "Earlier", "MicroDvdDocumentSortsCues");
        check(Parse("{0}{25}A &amp; B", 25).Cues[0].Text == "A &amp; B", "MicroDvdPreservesLiteralText");

        const string frames = "Invalid frame range; expected {start}{end} with nonnegative Int64 frames and end greater than start.";
        const string rate = "The frame rate must be finite and greater than zero.";
        const string range = "The converted frame range exceeds TimeSpan limits or collapses at tick precision.";
        const string missing = "No frame rate is available; supply one explicitly or use an initial {0}{0} or {1}{1} declaration.";
        const string style = "MicroDVD style and control tags in braces are not supported.";
        Reject("\n{0}{25}No rate", null, 2, missing, check, "MicroDvdMissingRateHasLine");
        Reject("\n{1}{1}25\n{0}{25}Conflict", 24, 2, "The declared frame rate conflicts with the caller-supplied frame rate.", check, "MicroDvdRateConflict");
        Reject("{1}{1}0\n{0}{25}A", null, 1, rate, check, "MicroDvdZeroDeclaredRate");
        Reject("{0}{0}NaN\n{0}{25}A", null, 1, rate, check, "MicroDvdNaNDeclaredRate");
        Reject("{1}{1}Infinity\n{0}{25}A", null, 1, rate, check, "MicroDvdInfiniteDeclaredRate");
        Reject("{1}{1}-25\n{0}{25}A", null, 1, rate, check, "MicroDvdNegativeDeclaredRate");
        Reject("{0}{25}A", 0, 1, rate, check, "MicroDvdZeroExplicitRate");
        Reject("{0}{25}A", -25, 1, rate, check, "MicroDvdNegativeExplicitRate");
        Reject("{0}{25}A", double.NaN, 1, rate, check, "MicroDvdNaNExplicitRate");
        Reject("{0}{25}A", double.PositiveInfinity, 1, rate, check, "MicroDvdInfiniteExplicitRate");
        Reject("{0}{25}A", double.NegativeInfinity, 1, rate, check, "MicroDvdNegativeInfiniteExplicitRate");
        Reject("{-1}{25}A", 25, 1, frames, check, "MicroDvdNegativeFrame");
        Reject("{25}{24}A", 25, 1, frames, check, "MicroDvdReversedFrames");
        Reject("{2}{2}A", 25, 1, frames, check, "MicroDvdEqualFrames");
        Reject("{0}{9223372036854775808}A", 25, 1, frames, check, "MicroDvdFrameIntegerOverflow");
        Reject("{0}{1.5}A", 25, 1, frames, check, "MicroDvdFractionalFrame");
        Reject("{0}{25A", 25, 1, frames, check, "MicroDvdMissingBrace");
        Reject("Untimed", 25, 1, frames, check, "MicroDvdUntimedText");
        Reject("{0}{9223372036854775807}A", 1, 1, range, check, "MicroDvdTimeOverflow");
        Reject("{0}{1}A", double.Epsilon, 1, range, check, "MicroDvdTinyRateOverflow");
        Reject("{0}{1}A", double.MaxValue, 1, range, check, "MicroDvdCollapsedTickRange");
        Reject("{9223372036854775806}{9223372036854775807}A", 10000000, 1, range, check, "MicroDvdRoundedInt64Boundary");
        Reject("{1}{1}25\n{0}{25}{y:i}A", null, 2, style, check, "MicroDvdRejectLocalStyle");
        Reject("{0}{25}{Y:b}A", 25, 1, style, check, "MicroDvdRejectGlobalStyle");
        Reject("{0}{25}A|{c:$FFFFFF}B", 25, 1, style, check, "MicroDvdRejectEmbeddedStyle");
        Reject("{0}{25}{unknown:A}B", 25, 1, style, check, "MicroDvdRejectUnknownControl");
        Reject("{0}{25}|  |", 25, 1, "The subtitle text must not be empty.", check, "MicroDvdEmptyCueText");
        Reject("{0}{25}A\n{1}{1}25", 25, 2, frames, check, "MicroDvdRejectLateDeclaration");
        Reject("", 25, 1, "No subtitle cues were found.", check, "MicroDvdEmptyFile");
        Reject("{1}{1}25", null, 1, "No subtitle cues were found.", check, "MicroDvdDeclarationOnly");
    }

    private static SubtitleDocument Parse(string text, double? frameRate = null) =>
        SubtitleReader.Parse(text, SubtitleFormat.MicroDvd, frameRate: frameRate);

    private static void Reject(string text, double? frameRate, int line, string reason, Action<bool, string> check, string name)
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        bool rejected = false;
        try
        {
            // Keep exact diagnostic assertions independent of the machine's UI language.
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            Parse(text, frameRate);
        }
        catch (FormatException exception)
        {
            rejected = exception.Message == string.Format(CultureInfo.CurrentCulture, "MicroDVD line {0}: {1}", line, reason);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
        check(rejected, name);
    }
}
