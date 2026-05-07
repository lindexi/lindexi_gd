using KadefihalldokaiChairwedone.CoursewareSpeechGenerators.ScriptParsers;

using Microsoft.Extensions.AI;

using System.Net.Http;

using VideoComposerLib;

using VolcEngineSdk.OpenSpeech;
using VolcEngineSdk.OpenSpeech.Contexts;

namespace KadefihalldokaiChairwedone.CoursewareSpeechGenerators;

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
    /// 生成讲稿脚本
    /// </summary>
    /// <param name="coursewareMaterialInfo"></param>
    /// <param name="chatClient"></param>
    /// <returns></returns>
    public async Task<CoursewareSpeechInfo> GenerateSpeechAsync(CoursewareMaterialInfo coursewareMaterialInfo, IChatClient chatClient)
    {
        // 调用大模型输出脚本
        throw new NotImplementedException();
    }

    /// <summary>
    /// 生成课件讲稿视频
    /// </summary>
    /// <param name="coursewareMaterialInfo"></param>
    /// <param name="chatClient"></param>
    /// <returns>讲稿视频</returns>
    public async Task<FileInfo> GeneratorCoursewareSpeechVideoAsync(CoursewareMaterialInfo coursewareMaterialInfo,
        IChatClient chatClient)
    {
        var coursewareSpeechInfo = await GenerateSpeechAsync(coursewareMaterialInfo, chatClient);
        var outputVideoFile = new FileInfo(Path.Join(WorkingDirectory.FullName, $"courseware_speech_{DateTime.Now:yyyyMMddHHmmss}.mp4"));
        await GeneratorCoursewareSpeechVideoAsync(coursewareSpeechInfo, outputVideoFile);
        return outputVideoFile;
    }

    /// <summary>
    /// 生成课件讲稿视频，输入是每一页的讲稿文本和页面截图，输出是合成好的视频文件
    /// </summary>
    public async Task GeneratorCoursewareSpeechVideoAsync(CoursewareSpeechInfo coursewareSpeechInfo, FileInfo outputVideoFile)
    {
        ArgumentNullException.ThrowIfNull(coursewareSpeechInfo);

        if (coursewareSpeechInfo.SlideInfoList.Count == 0)
        {
            throw new ArgumentException("页面列表不能为空。", nameof(coursewareSpeechInfo));
        }

        WorkingDirectory.Create();
        outputVideoFile.Directory?.Create();

        var parser = new ScriptParser();
        var videoSegments = new List<VideoSegment>(coursewareSpeechInfo.SlideInfoList.Count);

        for (var i = 0; i < coursewareSpeechInfo.SlideInfoList.Count; i++)
        {
            var slideInfo = coursewareSpeechInfo.SlideInfoList[i];
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

        var composeResult = await FFmpegVideoComposer.ComposeAsync(videoSegments, outputVideoFile).ConfigureAwait(false);
        if (!composeResult || !File.Exists(outputVideoFile.FullName))
        {
            throw new InvalidOperationException($"生成课件视频失败：{outputVideoFile.FullName}");
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
/// 课件页面材料信息，包括页面截图和页面内容文本
/// </summary>
/// <param name="SlideThumbnailFilePath"></param>
/// <param name="ContentText"></param>
public record CoursewareSlideMaterialInfo(FileInfo SlideThumbnailFilePath, string ContentText);

public record CoursewareMaterialInfo(List<CoursewareSlideMaterialInfo> SlideMaterialInfoList);