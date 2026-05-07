using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using VideoComposerLib;
using VolcEngineSdk.OpenSpeech;
using VolcEngineSdk.OpenSpeech.Contexts;

namespace KadefihalldokaiChairwedone;

public class CoursewareSpeechGenerator
{
    /// <summary>
    /// 工作路径，用于存放生成过程中的临时文件，如中间音频文件、视频文件等。使用系统临时目录或指定目录均可。
    /// </summary>
    public required DirectoryInfo WorkingDirectory { get; init; }

    public required FFmpegVideoComposer FFmpegVideoComposer { get; init; }

    /// <summary>
    /// OpenSpeech 语音合成客户端。
    /// </summary>
    public required OpenSpeechClient OpenSpeechClient { get; init; }

    /// <summary>
    /// 语音合成配置。
    /// </summary>
    public required CoursewareSpeechSynthesisOptions SpeechSynthesisOptions { get; init; }

    /// <summary>
    /// 生成课件讲稿视频，输入是每一页的讲稿文本和页面截图，输出是合成好的视频文件
    /// </summary>
    public async Task GeneratorCoursewareSpeechVideo(CoursewareSpeechInput coursewareSpeechInput)
    {
        ArgumentNullException.ThrowIfNull(coursewareSpeechInput);

        if (coursewareSpeechInput.SlideInfoList.Count == 0)
        {
            throw new ArgumentException("页面列表不能为空。", nameof(coursewareSpeechInput));
        }

        WorkingDirectory.Create();
        coursewareSpeechInput.OutputVideoFile.Directory?.Create();

        var parser = new ScriptParser();
        var videoSegments = new List<VideoSegment>(coursewareSpeechInput.SlideInfoList.Count);

        for (var i = 0; i < coursewareSpeechInput.SlideInfoList.Count; i++)
        {
            var slideInfo = coursewareSpeechInput.SlideInfoList[i];
            if (!slideInfo.SlideThumbnailFile.Exists)
            {
                throw new FileNotFoundException("页面截图文件不存在。", slideInfo.SlideThumbnailFile.FullName);
            }

            var scriptParseResult = parser.Parse(slideInfo.PlainScriptText);
            var slideDirectory = new DirectoryInfo(Path.Combine(WorkingDirectory.FullName, $"slide_{i + 1:D4}"));
            slideDirectory.Create();

            var audioFileList = await GenerateSlideAudioFilesAsync(scriptParseResult, slideDirectory).ConfigureAwait(false);
            videoSegments.Add(new VideoSegment(slideInfo.SlideThumbnailFile, audioFileList));
        }

        var composeResult = await FFmpegVideoComposer.ComposeAsync(videoSegments, coursewareSpeechInput.OutputVideoFile).ConfigureAwait(false);
        if (!composeResult || !coursewareSpeechInput.OutputVideoFile.Exists)
        {
            throw new InvalidOperationException($"生成课件视频失败：{coursewareSpeechInput.OutputVideoFile.FullName}");
        }
    }

    private async Task<IReadOnlyList<FileInfo>> GenerateSlideAudioFilesAsync(ScriptParseResult scriptParseResult, DirectoryInfo slideDirectory)
    {
        var audioFileList = new List<FileInfo>();
        var audioIndex = 0;

        foreach (var scriptInput in scriptParseResult.ScriptInputList)
        {
            foreach (var segment in scriptInput.Segments)
            {
                var audioFile = new FileInfo(Path.Combine(slideDirectory.FullName, $"audio_{audioIndex++:D4}.{SpeechSynthesisOptions.AudioFormat}"));
                var request = CreateRequest(segment.Text, scriptInput.ContextText, segment.PauseAfter);
                var options = new SpeechSynthesisRequestOptions(SpeechSynthesisOptions.Authentication)
                {
                    Protocol = SpeechSynthesisProtocol.HttpChunked,
                    RequestId = Guid.NewGuid().ToString(),
                    UsageTokensToReturn = SpeechSynthesisOptions.UsageTokensToReturn
                };

                var result = await OpenSpeechClient.SynthesizeAsync(request, options).ConfigureAwait(false);
                await File.WriteAllBytesAsync(audioFile.FullName, result.AudioData).ConfigureAwait(false);
                audioFileList.Add(audioFile);
            }
        }

        if (audioFileList.Count == 0)
        {
            throw new InvalidOperationException($"脚本未生成任何音频片段：{slideDirectory.FullName}");
        }

        return audioFileList;
    }

    private SpeechSynthesisRequest CreateRequest(string text, string? contextText, TimeSpan? pauseAfter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        return new SpeechSynthesisRequest
        {
            User = new UserMeta
            {
                Uid = Environment.UserName
            },
            RequestParameters = new SpeechSynthesisRequestParameters
            {
                Text = text,
                Model = SpeechSynthesisOptions.Model,
                Speaker = SpeechSynthesisOptions.Speaker,
                AudioParameters = new SpeechSynthesisAudioParameters
                {
                    Format = SpeechSynthesisOptions.AudioFormat,
                    SampleRate = SpeechSynthesisOptions.SampleRate
                },
                Additions = new SpeechSynthesisAdditions
                {
                    ContextTexts = string.IsNullOrWhiteSpace(contextText) ? null : [contextText],
                    SilenceDuration = pauseAfter is null ? null : (int) Math.Round(pauseAfter.Value.TotalMilliseconds)
                }
            }
        };
    }
}

/// <summary>
/// 课件讲稿视频生成输入参数。
/// </summary>
/// <param name="SlideInfoList">页面脚本和截图列表。</param>
/// <param name="OutputVideoFile">输出视频文件。</param>
public record CoursewareSpeechInput(IReadOnlyList<CoursewareSpeechSlideInfo> SlideInfoList, FileInfo OutputVideoFile);

/// <summary>
/// 语音合成配置。
/// </summary>
/// <param name="Authentication">鉴权信息。</param>
/// <param name="Speaker">发音人。</param>
/// <param name="Model">模型版本。</param>
/// <param name="AudioFormat">输出音频格式。</param>
/// <param name="SampleRate">输出采样率。</param>
/// <param name="UsageTokensToReturn">返回的用量标记。</param>
public record CoursewareSpeechSynthesisOptions(
    OpenSpeechAuthentication Authentication,
    string Speaker,
    string Model,
    string AudioFormat = "mp3",
    int SampleRate = 24000,
    string UsageTokensToReturn = "text_words");

/// <summary>
/// 课件讲稿的页面信息
/// </summary>
/// <param name="PlainScriptText">原始的脚本内容，格式如 [上下文: 用轻松亲切的语气，语速适中]
/// 同学们好，欢迎来到今天的六年级语文下册课堂。[停顿: 1秒]</param>
/// <param name="SlideThumbnailFile">页面截图内容</param>
public record CoursewareSpeechSlideInfo(string PlainScriptText, FileInfo SlideThumbnailFile);

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

record ScriptParseResult(List<ScriptFormatInfo> ScriptFormatInfoList, List<ScriptInput> ScriptInputList);

record ScriptFormatInfo(Dictionary<string, string> FormatDictionary, string Text);

/// <summary>
/// 脚本信息
/// </summary>
/// <param name="ContextText">上下文信息</param>
/// <param name="Segments">讲话的段落</param>
internal sealed record ScriptInput(string? ContextText, IReadOnlyList<SpeechSegment> Segments);

/// <summary>
/// 讲话的段落
/// </summary>
/// <param name="Text">文本</param>
/// <param name="PauseAfter">停顿时间</param>
internal sealed record SpeechSegment(string Text, TimeSpan? PauseAfter);