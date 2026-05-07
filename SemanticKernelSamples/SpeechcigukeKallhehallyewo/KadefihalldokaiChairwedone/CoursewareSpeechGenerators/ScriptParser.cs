using System.Globalization;
using System.Text.RegularExpressions;

namespace KadefihalldokaiChairwedone.CoursewareSpeechGenerators;

class ScriptParser
{
    private static readonly Regex FormatRegex = new(@"\[(?<key>[^:\]]+)\s*:\s*(?<value>[^\]]+)\]", RegexOptions.Compiled);

    public ScriptParseResult Parse(string plainScriptText)
    {
        ArgumentNullException.ThrowIfNull(plainScriptText);

        var scriptFormatInfoList = ParseFormatInfoList(plainScriptText);
        var scriptInputList = ParseScriptInputList(scriptFormatInfoList);

        return new ScriptParseResult(scriptFormatInfoList, scriptInputList);
    }

    private static List<ScriptFormatInfo> ParseFormatInfoList(string plainScriptText)
    {
        var scriptFormatInfoList = new List<ScriptFormatInfo>();
        var pendingFormatDictionary = new Dictionary<string, string>(StringComparer.Ordinal);
        var startIndex = 0;

        foreach (Match match in FormatRegex.Matches(plainScriptText))
        {
            var text = plainScriptText[startIndex..match.Index].Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                scriptFormatInfoList.Add(new ScriptFormatInfo(new Dictionary<string, string>(pendingFormatDictionary, StringComparer.Ordinal), text));
                pendingFormatDictionary.Clear();
            }

            var key = match.Groups["key"].Value.Trim();
            var value = match.Groups["value"].Value.Trim();
            pendingFormatDictionary[key] = value;

            startIndex = match.Index + match.Length;
        }

        var trailingText = plainScriptText[startIndex..].Trim();
        if (!string.IsNullOrWhiteSpace(trailingText) || pendingFormatDictionary.Count > 0)
        {
            scriptFormatInfoList.Add(new ScriptFormatInfo(new Dictionary<string, string>(pendingFormatDictionary, StringComparer.Ordinal), trailingText));
        }

        return scriptFormatInfoList;
    }

    private static List<ScriptInput> ParseScriptInputList(List<ScriptFormatInfo> scriptFormatInfoList)
    {
        var scriptInputList = new List<ScriptInput>();
        var speechSegmentList = new List<SpeechSegment>();
        string? contextText = null;

        foreach (var scriptFormatInfo in scriptFormatInfoList)
        {
            if (scriptFormatInfo.FormatDictionary.TryGetValue("停顿", out var pauseText))
            {
                ApplyPause(speechSegmentList, pauseText);
            }

            if (scriptFormatInfo.FormatDictionary.TryGetValue("上下文", out var currentContextText))
            {
                if (speechSegmentList.Count > 0)
                {
                    scriptInputList.Add(new ScriptInput(contextText, speechSegmentList.ToList()));
                    speechSegmentList.Clear();
                }

                contextText = currentContextText;
            }

            if (!string.IsNullOrWhiteSpace(scriptFormatInfo.Text))
            {
                speechSegmentList.Add(new SpeechSegment(scriptFormatInfo.Text, null));
            }
        }

        if (speechSegmentList.Count > 0)
        {
            scriptInputList.Add(new ScriptInput(contextText, speechSegmentList.ToList()));
        }

        return scriptInputList;
    }

    private static void ApplyPause(List<SpeechSegment> speechSegmentList, string pauseText)
    {
        if (speechSegmentList.Count == 0)
        {
            return;
        }

        var pauseAfter = ParsePause(pauseText);
        var lastSpeechSegment = speechSegmentList[^1];
        var mergedPauseAfter = lastSpeechSegment.PauseAfter is { } existingPauseAfter
            ? existingPauseAfter + pauseAfter
            : pauseAfter;

        speechSegmentList[^1] = lastSpeechSegment with { PauseAfter = mergedPauseAfter };
    }

    private static TimeSpan ParsePause(string pauseText)
    {
        if (string.IsNullOrWhiteSpace(pauseText))
        {
            throw new ArgumentException("停顿时长不能为空。", nameof(pauseText));
        }

        const string suffix = "秒";
        var normalizedPauseText = pauseText.Trim();
        if (!normalizedPauseText.EndsWith(suffix, StringComparison.Ordinal))
        {
            throw new FormatException($"无法解析停顿时长：{pauseText}");
        }

        var secondsText = normalizedPauseText[..^suffix.Length].Trim();
        if (!double.TryParse(secondsText, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            throw new FormatException($"无法解析停顿时长：{pauseText}");
        }

        return TimeSpan.FromSeconds(seconds);
    }
}