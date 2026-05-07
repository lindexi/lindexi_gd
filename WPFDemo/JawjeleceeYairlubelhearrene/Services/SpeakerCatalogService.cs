using System.IO;
using System.IO;
using System.Text.RegularExpressions;

namespace JawjeleceeYairlubelhearrene;

internal static partial class SpeakerCatalogService
{
    public static IReadOnlyList<SpeakerOption> LoadSpeakerOptions()
    {
        var catalogPath = Path.Combine(AppContext.BaseDirectory, "Docs", "VolcEngineSdk", "在线音色列表.txt");
        if (!File.Exists(catalogPath))
        {
            return [new SpeakerOption("Vivi 2.0", "zh_female_vv_uranus_bigtts", "中文、日文、印尼、墨西哥西班牙语", "通用场景")];
        }

        var content = File.ReadAllText(catalogPath);
        var section = ExtractSection(content, "豆包语音合成模型2.0", "端到端实时语音大模型");
        var speakerList = new List<SpeakerOption>();

        foreach (Match match in SpeakerLineRegex().Matches(section))
        {
            var scenario = match.Groups["scenario"].Value.Trim();
            var displayName = match.Groups["name"].Value.Trim();
            var voiceType = match.Groups["voiceType"].Value.Trim();
            var language = match.Groups["language"].Value.Trim();
            var capabilities = match.Groups["capabilities"].Value.Trim();

            if (!string.Equals(scenario, "通用场景", StringComparison.Ordinal) ||
                !capabilities.Contains("情感变化", StringComparison.Ordinal) ||
                !capabilities.Contains("指令遵循", StringComparison.Ordinal) ||
                !capabilities.Contains("ASMR", StringComparison.Ordinal))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(voiceType))
            {
                continue;
            }

            speakerList.Add(new SpeakerOption(displayName, voiceType, language, scenario));
        }

        if (speakerList.All(t => !string.Equals(t.VoiceType, "zh_female_vv_uranus_bigtts", StringComparison.Ordinal)))
        {
            speakerList.Insert(0, new SpeakerOption("Vivi 2.0", "zh_female_vv_uranus_bigtts", "中文、日文、印尼、墨西哥西班牙语", "通用场景"));
        }

        return speakerList
            .DistinctBy(t => t.VoiceType, StringComparer.Ordinal)
            .OrderBy(t => t.DisplayName, StringComparer.Ordinal)
            .ToArray();
    }

    private static string ExtractSection(string content, string startMark, string endMark)
    {
        var startIndex = content.IndexOf(startMark, StringComparison.Ordinal);
        if (startIndex < 0)
        {
            return content;
        }

        var endIndex = content.IndexOf(endMark, startIndex, StringComparison.Ordinal);
        if (endIndex < 0)
        {
            endIndex = content.Length;
        }

        return content[startIndex..endIndex];
    }

    [GeneratedRegex(@"^\|\s*(?<scenario>[^|]+?)\s*\|\s*(?<name>[^|]+?)\s*\|\s*(?<voiceType>[^|]+?)\s*\|\s*(?<language>[^|]+?)\s*\|\s*(?<capabilities>[^|]+?)\s*\|", RegexOptions.Multiline)]
    private static partial Regex SpeakerLineRegex();
}
