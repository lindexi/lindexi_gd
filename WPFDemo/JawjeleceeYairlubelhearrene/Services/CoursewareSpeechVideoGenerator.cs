using System.ClientModel;
using System.IO;
using System.Net.Sockets;
using System.Text;
using JawjeleceeYairlubelhearrene.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using VideoComposerLib;
using VolcEngineSdk.OpenSpeech;
using VolcEngineSdk.OpenSpeech.Contexts;

namespace JawjeleceeYairlubelhearrene.Services;

internal sealed class CoursewareSpeechVideoGenerator
{
    private readonly System.Net.Http.HttpClient _httpClient = new();

    /// <summary>
    /// 根据页面文本和截图生成讲稿视频。
    /// </summary>
    public async Task<SpeechVideoGenerationResult> GenerateAsync(
        CoursewareMaterialInfo coursewareMaterialInfo,
        SpeechVideoGenerationOptions options,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(coursewareMaterialInfo);
        ArgumentNullException.ThrowIfNull(options);

        if (coursewareMaterialInfo.SlideMaterialInfoList.Count == 0)
        {
            throw new ArgumentException("页面列表不能为空。", nameof(coursewareMaterialInfo));
        }

        var openAiClient = new OpenAIClient(new ApiKeyCredential(options.OpenAiApiKey), new OpenAIClientOptions
        {
            Endpoint = options.OpenAiEndpoint,
            NetworkTimeout = TimeSpan.FromHours(1)
        });

        var chatClient = openAiClient.GetChatClient(options.OpenAiModel).AsIChatClient();
        var authentication = OpenSpeechAuthentication.CreateWithApiKey(options.OpenSpeechApiKey, options.ResourceId);
        var synthesisOptions = new CoursewareSpeechSynthesisOptions(authentication, options.Speaker);
        var logger = new ProgressLogger(progress);

        await using var ffmpegVideoComposer = new FFmpegVideoComposer(
            options.FfmpegExecutableFile,
            workingDirectory: options.OutputDirectory,
            logHandler: (level, message) => progress?.Report($"[{level}] {message}"));

        var speechInfo = await GenerateSpeechAsync(coursewareMaterialInfo, chatClient, logger, progress, cancellationToken);
        var outputVideoFile = new System.IO.FileInfo(System.IO.Path.Combine(options.OutputDirectory.FullName, $"courseware_speech_{DateTime.Now:yyyyMMddHHmmss}.mp4"));
        await GenerateVideoAsync(speechInfo, outputVideoFile, synthesisOptions, ffmpegVideoComposer, logger, progress, cancellationToken);
        return new SpeechVideoGenerationResult(outputVideoFile, options.OutputDirectory, speechInfo);
    }

    private async Task<CoursewareSpeechInfo> GenerateSpeechAsync(
        CoursewareMaterialInfo coursewareMaterialInfo,
        IChatClient chatClient,
        ILogger logger,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var allCoursewareText = BuildAllCoursewareText(coursewareMaterialInfo);
        var previousScriptsBuilder = new StringBuilder();
        var slideInfoList = new List<CoursewareSpeechSlideInfo>(coursewareMaterialInfo.SlideMaterialInfoList.Count);

        for (var i = 0; i < coursewareMaterialInfo.SlideMaterialInfoList.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var slideMaterialInfo = coursewareMaterialInfo.SlideMaterialInfoList[i];
            var slideIndex = i + 1;
            logger.LogInformation($"开始生成第 {slideIndex} 页讲稿。");
            progress?.Report($"正在生成第 {slideIndex}/{coursewareMaterialInfo.SlideMaterialInfoList.Count} 页讲稿...");

            var plainScriptText = await GenerateSlideScriptAsync(
                chatClient,
                allCoursewareText,
                slideIndex,
                slideMaterialInfo.ContentText,
                previousScriptsBuilder.ToString(),
                slideMaterialInfo.SlideThumbnailFilePath,
                cancellationToken);

            slideInfoList.Add(new CoursewareSpeechSlideInfo(plainScriptText, slideMaterialInfo.SlideThumbnailFilePath));
            previousScriptsBuilder.AppendLine($"---第 {slideIndex} 页---");
            previousScriptsBuilder.AppendLine(plainScriptText);
            logger.LogInformation($"完成生成第 {slideIndex} 页讲稿。");
        }

        return new CoursewareSpeechInfo(slideInfoList);
    }

    private async Task GenerateVideoAsync(
        CoursewareSpeechInfo coursewareSpeechInfo,
        System.IO.FileInfo outputVideoFile,
        CoursewareSpeechSynthesisOptions synthesisOptions,
        FFmpegVideoComposer ffmpegVideoComposer,
        ILogger logger,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(coursewareSpeechInfo);

        var workingDirectory = new System.IO.DirectoryInfo(System.IO.Path.Combine(outputVideoFile.DirectoryName!, "Working"));
        workingDirectory.Create();
        outputVideoFile.Directory?.Create();

        var parser = new ScriptParser();
        var videoSegments = new List<VideoSegment>(coursewareSpeechInfo.SlideInfoList.Count);
        var openSpeechClient = new OpenSpeechClient(_httpClient);

        for (var i = 0; i < coursewareSpeechInfo.SlideInfoList.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var slideInfo = coursewareSpeechInfo.SlideInfoList[i];
            var scriptParseResult = parser.Parse(slideInfo.PlainScriptText);
            var slideDirectory = new System.IO.DirectoryInfo(System.IO.Path.Combine(workingDirectory.FullName, $"slide_{i + 1:D4}"));
            slideDirectory.Create();

            progress?.Report($"正在合成第 {i + 1}/{coursewareSpeechInfo.SlideInfoList.Count} 页语音...");
            logger.LogInformation($"开始合成第 {i + 1} 页语音。");
            var audioFileList = await GenerateSlideAudioFilesAsync(scriptParseResult, slideDirectory, synthesisOptions, openSpeechClient, logger, cancellationToken);
            videoSegments.Add(new VideoSegment(slideInfo.SlideThumbnailFile, audioFileList));
        }

        progress?.Report("正在使用 FFmpeg 合成最终视频...");
        var composeResult = await ffmpegVideoComposer.ComposeAsync(videoSegments, outputVideoFile, cancellationToken);
        if (!composeResult || !File.Exists(outputVideoFile.FullName))
        {
            throw new InvalidOperationException($"生成讲稿视频失败：{outputVideoFile.FullName}");
        }
    }

    private async Task<IReadOnlyList<FileInfo>> GenerateSlideAudioFilesAsync(
        ScriptParseResult scriptParseResult,
        System.IO.DirectoryInfo slideDirectory,
        CoursewareSpeechSynthesisOptions synthesisOptions,
        OpenSpeechClient openSpeechClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var audioFileList = new List<System.IO.FileInfo>();
        var audioIndex = 0;

        foreach (var scriptInput in scriptParseResult.ScriptInputList)
        {
            foreach (var segment in scriptInput.Segments)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var audioFile = new System.IO.FileInfo(System.IO.Path.Combine(slideDirectory.FullName, $"audio_{audioIndex++:D4}.{synthesisOptions.AudioFormat}"));
                var request = CreateSpeechRequest(segment.Text, scriptInput.ContextText, segment.PauseAfter, synthesisOptions);
                var requestId = Guid.NewGuid().ToString();
                var options = new SpeechSynthesisRequestOptions(synthesisOptions.Authentication)
                {
                    Protocol = SpeechSynthesisProtocol.HttpChunked,
                    RequestId = requestId,
                    UsageTokensToReturn = synthesisOptions.UsageTokensToReturn
                };

                logger.LogInformation($"开始生成语音 Id={requestId}。");
                var result = await openSpeechClient.SynthesizeAsync(request, options, cancellationToken);
                await File.WriteAllBytesAsync(audioFile.FullName, result.AudioData, cancellationToken);
                audioFileList.Add(audioFile);
            }
        }

        if (audioFileList.Count == 0)
        {
            throw new InvalidOperationException($"脚本未生成任何音频片段：{slideDirectory.FullName}");
        }

        return audioFileList;
    }

    private static SpeechSynthesisRequest CreateSpeechRequest(
        string text,
        string? contextText,
        TimeSpan? pauseAfter,
        CoursewareSpeechSynthesisOptions synthesisOptions)
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
                Speaker = synthesisOptions.Speaker,
                AudioParameters = new SpeechSynthesisAudioParameters
                {
                    Format = synthesisOptions.AudioFormat,
                    SampleRate = synthesisOptions.SampleRate
                },
                Additions = new SpeechSynthesisAdditions
                {
                    ContextTexts = string.IsNullOrWhiteSpace(contextText) ? null : [contextText],
                    SilenceDuration = pauseAfter is null ? null : (int)Math.Round(pauseAfter.Value.TotalMilliseconds)
                }
            }
        };
    }

    private static async Task<string> GenerateSlideScriptAsync(
        IChatClient chatClient,
        string allCoursewareText,
        int slideIndex,
        string currentPageText,
        string previousScripts,
        System.IO.FileInfo slideThumbnailFile,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(chatClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(allCoursewareText);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPageText);
        ArgumentNullException.ThrowIfNull(slideThumbnailFile);

        var prompt = SlideScriptPromptTemplate.Replace("$(AllCoursewareText)", allCoursewareText)
            .Replace("$(SlideIndex)", slideIndex.ToString())
            .Replace("$(CurrentPageText)", currentPageText)
            .Replace("$(PreviousScripts)", previousScripts);

        var message = new ChatMessage(ChatRole.User,
        [
            new TextContent(prompt),
            CreateScreenshotContent(slideThumbnailFile)
        ]);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var responseText = await GetResponseTextAsync(chatClient, [message], cancellationToken);
                if (string.IsNullOrWhiteSpace(responseText))
                {
                    throw new InvalidOperationException($"第 {slideIndex} 页未生成有效讲稿脚本。");
                }

                return responseText.Trim();
            }
            catch (Exception exception) when (IsCanRetrySocketException(exception))
            {
            }
        }
    }

    private static string BuildAllCoursewareText(CoursewareMaterialInfo coursewareMaterialInfo)
    {
        var stringBuilder = new StringBuilder();
        for (var i = 0; i < coursewareMaterialInfo.SlideMaterialInfoList.Count; i++)
        {
            stringBuilder.AppendLine($"---第 {i + 1} 页---");
            stringBuilder.AppendLine(coursewareMaterialInfo.SlideMaterialInfoList[i].ContentText);
        }

        return stringBuilder.ToString();
    }

    private static async Task<string> GetResponseTextAsync(IChatClient chatClient, IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
    {
        var stringBuilder = new StringBuilder();

        await foreach (var update in chatClient.GetStreamingResponseAsync(messages, cancellationToken: cancellationToken))
        {
            foreach (var content in update.Contents)
            {
                if (content is TextContent textContent && !string.IsNullOrEmpty(textContent.Text))
                {
                    stringBuilder.Append(textContent.Text);
                }
            }
        }

        return stringBuilder.ToString();
    }

    private static DataContent CreateScreenshotContent(System.IO.FileInfo slideThumbnailFile)
    {
        var imageBytes = File.ReadAllBytes(slideThumbnailFile.FullName);
        var mediaType = GetImageMediaType(slideThumbnailFile.Extension);
        return new DataContent(imageBytes, mediaType);
    }

    private static string GetImageMediaType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            _ => throw new NotSupportedException($"暂不支持的图片格式：{extension}")
        };
    }

    private static bool IsCanRetrySocketException(Exception exception)
    {
        if (exception is SocketException socketException)
        {
            return socketException.SocketErrorCode != SocketError.ConnectionRefused;
        }

        if (exception is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                if (IsCanRetrySocketException(innerException))
                {
                    return true;
                }
            }
        }
        else if (exception.InnerException is { } innerException)
        {
            return IsCanRetrySocketException(innerException);
        }

        return false;
    }

    private const string SlideScriptPromptTemplate = """
你是一个专业的课件页面讲述脚本生成器。你的任务是根据当前页面的截图和文本信息，结合整份课件的上下文，为当前页面生成一份可直接用于语音合成的中文讲述脚本。

---

## 输入信息

你将收到以下输入：

1. **整份课件全部页面文本**：包含课件所有页面的文本提取内容，每页用 `---第 N 页---` 标明页码。
   ```
   $(AllCoursewareText)
   ```

2. **当前页面序号**：一个从 1 开始的整数。
   ```
   $(SlideIndex)
   ```

3. **当前页面文本**：当前页面提取到的所有文本框内容。
   ```
   $(CurrentPageText)
   ```

4. **之前页面已生成的讲述脚本**：当前页面之前所有页面已生成的脚本结果。如果当前是第一页，此项为空。
   ```
   $(PreviousScripts)
   ```

5. **当前页面截图**：以多模态图片形式提供，你可以直接查看页面布局、图片、配色等视觉信息。

---

## 核心任务

基于以上所有输入，为当前页面生成一份中文讲述脚本。该脚本将用于：
- 驱动语音合成引擎生成当前页的讲述音频
- 与当前页截图合成，最终形成课程视频片段

---

## 讲述脚本要求

### 1. 内容忠实性
- **严格基于截图和文本**：讲述内容必须忠实于当前页截图中的可见元素和文本提取信息，不得臆测或编造截图与文本中不存在的内容。
- **充分利用视觉信息**：截图中如果包含图片、图表、示意图、版式设计、按钮图标、高亮标记、颜色标注等视觉元素，都应在脚本中适当提及，使讲述与画面形成紧密呼应。不要忽略页面上的任何可见元素。
- **不要生成后续逻辑**：脚本仅服务于当前页面的讲述，不要包含视频合成、页面切换指令等制作层面的内容。

### 2. 脚本充实度
- **每页脚本必须足够充实**：内容不能过于简短，一页课件通常需要支撑 30 到 90 秒的讲述。
- **充分展开每个元素**：页面上的每一项内容都要展开讲解，不要一句话草草带过。
- **增加教学引导语**：在讲解中适当加入引导、设问、鼓励、总结等教师常用语，让讲述更有课堂氛围。

### 3. 上下文衔接
- **结合整份课件文本**：了解课件整体结构和前后页内容，确保当前页讲述在全局脉络中位置恰当。
- **承接前文脚本**：参考此前页面已生成的脚本，让当前页的讲述自然承接上文。
- **为后续铺垫**：在页末可以自然地为下一页内容埋下伏笔或过渡。

### 4. 讲述风格
- 采用**教师授课的口吻**，亲切自然、条理清晰，适合学生学习理解。
- 可以适当使用"我们""大家""同学们"等课堂称呼，营造临场感。

---

## 操作符说明

脚本中必须嵌入操作符，用于控制语音合成的细节表现。操作符使用 `[]` 包裹，格式为 `[Key: Value]`。

### 当前可用的操作符：

#### ① 停顿操作符 `[停顿: N秒]`
在语音中插入指定时长的停顿。每一段讲述都应根据内容自然加入停顿，避免整篇脚本毫无节奏。

#### ② 上下文操作符 `[上下文: ...]`
向语音合成引擎传递辅助信息，用于调整合成语音的表现效果，使语音更具情感和表现力。

---

## 输出格式

- 你必须输出**纯脚本文本**，可直接用于语音合成引擎。**不要使用 Markdown 格式**。
- 脚本中嵌入的操作符使用 `[Key: Value]` 格式，原样保留在文本中。
- 脚本整体应自然流畅，操作符与讲述文字融为一体。

---

## 生成流程

1. 观察截图。
2. 阅读文本。
3. 回顾前文。
4. 构思脚本。
5. 嵌入操作符。
6. 输出脚本。
""";

    private sealed class ProgressLogger(IProgress<string>? progress) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            progress?.Report($"[{logLevel}] {message}");
        }
    }

    private sealed class ScriptParser
    {
        private static readonly System.Text.RegularExpressions.Regex FormatRegex = new(@"\[(?<key>[^:\]]+)\s*:\s*(?<value>[^\]]+)\]", System.Text.RegularExpressions.RegexOptions.Compiled);

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

            foreach (System.Text.RegularExpressions.Match match in FormatRegex.Matches(plainScriptText))
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
            if (!double.TryParse(secondsText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var seconds))
            {
                throw new FormatException($"无法解析停顿时长：{pauseText}");
            }

            return TimeSpan.FromSeconds(seconds);
        }
    }

    private sealed record ScriptFormatInfo(Dictionary<string, string> FormatDictionary, string Text);
    private sealed record ScriptParseResult(List<ScriptFormatInfo> ScriptFormatInfoList, List<ScriptInput> ScriptInputList);
    private sealed record ScriptInput(string? ContextText, IReadOnlyList<SpeechSegment> Segments);
    private sealed record SpeechSegment(string Text, TimeSpan? PauseAfter);
}
