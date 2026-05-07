using KadefihalldokaiChairwedone.CoursewareSpeechGenerators.ScriptParsers;

using Microsoft.Extensions.AI;

using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VideoComposerLib;

using VolcEngineSdk.OpenSpeech;
using VolcEngineSdk.OpenSpeech.Contexts;

namespace KadefihalldokaiChairwedone.CoursewareSpeechGenerators;

public class CoursewareSpeechGenerator
{
    public ILogger Logger { get; init; } = NullLogger.Instance;

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
        ArgumentNullException.ThrowIfNull(coursewareMaterialInfo);
        ArgumentNullException.ThrowIfNull(chatClient);

        if (coursewareMaterialInfo.SlideMaterialInfoList.Count == 0)
        {
            throw new ArgumentException("页面列表不能为空。", nameof(coursewareMaterialInfo));
        }

        var allCoursewareText = BuildAllCoursewareText(coursewareMaterialInfo);
        var previousScriptsBuilder = new StringBuilder();
        var slideInfoList = new List<CoursewareSpeechSlideInfo>(coursewareMaterialInfo.SlideMaterialInfoList.Count);

        for (var i = 0; i < coursewareMaterialInfo.SlideMaterialInfoList.Count; i++)
        {
            Logger.LogInformation($"开始生成第 {i} 页讲稿");

            var slideMaterialInfo = coursewareMaterialInfo.SlideMaterialInfoList[i];
            if (!slideMaterialInfo.SlideThumbnailFilePath.Exists)
            {
                throw new FileNotFoundException("页面截图文件不存在。", slideMaterialInfo.SlideThumbnailFilePath.FullName);
            }

            var slideIndex = i + 1;
            var plainScriptText = await GenerateSlideScriptAsync(
                chatClient,
                allCoursewareText,
                slideIndex,
                slideMaterialInfo.ContentText,
                previousScriptsBuilder.ToString(),
                slideMaterialInfo.SlideThumbnailFilePath).ConfigureAwait(false);

            slideInfoList.Add(new CoursewareSpeechSlideInfo(plainScriptText, slideMaterialInfo.SlideThumbnailFilePath));
            previousScriptsBuilder.AppendLine($"---第 {slideIndex} 页---");
            previousScriptsBuilder.AppendLine(plainScriptText);

            Logger.LogInformation($"完成生成第 {i} 页讲稿。讲稿内容：\r\n{plainScriptText}\r\n-----");
        }

        return new CoursewareSpeechInfo(slideInfoList);
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

            Logger.LogInformation($"开始合成第 {i} 页语音");

            var audioFileList = await GenerateSlideAudioFilesAsync(scriptParseResult, slideDirectory).ConfigureAwait(false);
            videoSegments.Add(new VideoSegment(slideInfo.SlideThumbnailFile, audioFileList));

            Logger.LogInformation($"完成合成第 {i} 页语音");
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
                var requestId = Guid.NewGuid().ToString();

                Logger.LogInformation($"开始生成语音 Id={requestId} 文本内容=`{segment.Text}` 上下文={scriptInput.ContextText ?? "NULL"}");

                var options = new SpeechSynthesisRequestOptions(SpeechSynthesisOptions.Authentication)
                {
                    Protocol = SpeechSynthesisProtocol.HttpChunked,
                    RequestId = requestId,
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

    private async Task<string> GenerateSlideScriptAsync(IChatClient chatClient, string allCoursewareText, int slideIndex,
        string currentPageText, string previousScripts, FileInfo slideThumbnailFile)
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
            try
            {
                var responseText = await GetResponseTextAsync(chatClient, [message]).ConfigureAwait(false);
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
        ArgumentNullException.ThrowIfNull(coursewareMaterialInfo);

        var stringBuilder = new StringBuilder();
        for (var i = 0; i < coursewareMaterialInfo.SlideMaterialInfoList.Count; i++)
        {
            stringBuilder.AppendLine($"---第 {i + 1} 页---");
            stringBuilder.AppendLine(coursewareMaterialInfo.SlideMaterialInfoList[i].ContentText);
        }

        return stringBuilder.ToString();
    }

    private static async Task<string> GetResponseTextAsync(IChatClient chatClient, IEnumerable<ChatMessage> messages)
    {
        var stringBuilder = new StringBuilder();

        await foreach (var update in chatClient.GetStreamingResponseAsync(messages).ConfigureAwait(false))
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

    private static DataContent CreateScreenshotContent(FileInfo slideThumbnailFile)
    {
        ArgumentNullException.ThrowIfNull(slideThumbnailFile);

        var imageBytes = File.ReadAllBytes(slideThumbnailFile.FullName);
        var mediaType = GetImageMediaType(slideThumbnailFile.Extension);
        return new DataContent(imageBytes, mediaType);
    }

    private static string GetImageMediaType(string extension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);

        return extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            _ => throw new NotSupportedException($"暂不支持的图片格式：{extension}"),
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
- **充分利用视觉信息**：截图中如果包含图片、图表、示意图、版式设计、按钮图标（如播放按钮、喇叭图标）、高亮标记、颜色标注等视觉元素，都应在脚本中适当提及（如"请看这幅插图""图中奶奶和小孙子围坐在桌前""注意页面右侧用红色标注的重点""看到这个播放按钮了吗"等），使讲述与画面形成紧密呼应。不要忽略页面上的任何可见元素。
- **不要生成后续逻辑**：脚本仅服务于当前页面的讲述，不要包含视频合成、页面切换指令等制作层面的内容。

### 2. 脚本充实度（重要）
- **每页脚本必须足够充实**：脚本将驱动当前页面在视频中的完整讲述，因此内容不能过于简短。一页课件在视频中通常停留 30 到 90 秒，脚本必须能够撑起这段时间的讲述。
- **字数参考**（不计操作符，仅计讲述文字）：
  - 标题页 / 导入页：**120 ~ 200 字**
  - 正文内容页 / 知识点页：**200 ~ 400 字**
  - 练习页 / 互动页：**180 ~ 350 字**
  - 总结页 / 过渡页：**100 ~ 180 字**
- **充分展开每个元素**：页面上的每一项内容都要展开讲解，不要一句话草草带过。
  - 标题要解读其含义和在本课中的位置。
  - 图片/插图要描述画面并点明与课文主题的关联。
  - 文字要点要逐一讲解，必要时做简要延伸或举例。
  - 按钮/图标要说明其用途，引导学生关注。
  - 生字词要逐个或分组带领认读，并提醒注意易错点。
  - 提问后要留出思考时间，并给出引导性或总结性的话语。
- **增加教学引导语**：在讲解中适当加入引导、设问、鼓励、总结等教师常用语，如"大家注意看这里""有没有同学能告诉老师……""很好，大家都注意到了""我们来总结一下""这一点非常重要，大家要记住哦"等，让讲述更有课堂氛围，同时也自然增加脚本长度。
- **避免过于精简**：不要追求"最简表达"。宁可稍微多说几句，也不要让听众感觉"一页一瞬间就过去了"。每个页面都应该有充分的展开和自然的收束。

### 3. 上下文衔接
- **结合整份课件文本**：了解课件整体结构和前后页内容，确保当前页讲述在全局脉络中位置恰当。
- **承接前文脚本**：参考此前页面已生成的脚本，让当前页的讲述自然承接上文。避免生硬的断层感，也不要机械重复前文已经详细说过的大段内容。可以用"刚才我们了解了……接下来……""上一页我们提到……现在我们进一步……"等方式自然过渡。如果是第一页，应有一个亲切自然的开场（如"同学们好，欢迎来到今天的语文课堂，今天我们要一起学习的是……"）。
- **为后续铺垫**：在页末可以自然地为下一页内容埋下伏笔或过渡（如"了解了这些基础知识后，接下来我们就要……""带着这个问题，我们进入下一页"），使整个课件的讲述串成一条流畅的线。

### 4. 讲述风格
- 采用**教师授课的口吻**，亲切自然、条理清晰，适合学生学习理解。语言要**口语化**，避免过于书面化的长句和生僻术语，确保适合朗读和聆听。
- 根据页面内容的性质灵活调整语气：标题页正式庄重、导入页轻松活泼、知识点页清晰讲解、提问页启发引导、练习页耐心细致、总结页归纳提炼。
- 可以适当使用"我们""大家""同学们"等课堂称呼，营造临场感。

---

## 操作符说明

脚本中必须嵌入操作符，用于控制语音合成的细节表现。操作符使用 `[]` 包裹，格式为 `[Key: Value]`。

### 当前可用的操作符：

#### ① 停顿操作符 `[停顿: N秒]`
在语音中插入指定时长的停顿。用于控制讲述节奏，使整篇脚本有呼吸感。

**使用场景：**
- 句子或段落之间的自然换气
- 重点内容前后的强调停顿
- 提问之后，给学生留出思考时间（建议 2~3 秒）
- 页面内容模块之间的过渡
- 标题与正文之间的分隔
- 词语认读时，在词组之间插入短停顿（0.5~1 秒）

**用法示例：**
```
今天我们来学习第二课，腊八粥。[停顿: 1秒]首先请大家看这幅图片。[停顿: 1.5秒]图上画的是什么呢？
```

**注意：**
- 停顿是**必须使用**的操作符。每一段讲述都应根据内容自然加入停顿，避免整篇脚本毫无节奏、一气到底。
- 一般每 1~3 句话后安排一个停顿。停顿时长在 0.5 秒到 3 秒之间，根据语义自然程度和教学需要选择。
- 知识点密集处可适当增加停顿频率和时长，让学生有时间消化。
- 停顿过少会让听众喘不过气、跟不上节奏；停顿过多过密也会打断语流。务必根据内容节奏合理分布。

#### ② 上下文操作符 `[上下文: ...]`
向语音合成引擎传递辅助信息，用于调整合成语音的表现效果，使语音更具情感和表现力。该操作符本身不会被朗读出来。

**Value 可以是以下类型：**

1. **语速调整**：如 `你可以说慢一点吗？`、`请说快一点`、`语速放慢一些，让学生听清楚`
2. **情绪/语气调整**：如 `你可以用特别特别痛心的语气说话吗？`、`嗯，你的语气再欢乐一点`、`请用严肃的语气`、`用温柔的语气来说`、`用略带神秘感的语气`
3. **音量调整**：如 `你可以小声一点吗？`、`你嗓门再小点`、`声音大一点，强调一下`
4. **音感调整**：如 `你用骄傲的语气来说话`、`请用惊讶的语气`、`用疑惑的口吻`、`用肯定有力的语气`
5. **关联上下文**：传递前后语境信息，帮助合成引擎理解当前语句的情感走向，从而产生更自然的朗读效果。

**使用场景：**
- 需要特别强调某个知识点时：`[上下文: 请说慢一点，语气坚定一些]`
- 讲述感人故事时：`[上下文: 用温暖、柔和的语气]`
- 提问时：`[上下文: 用启发式的、略带疑问的语气]`
- 导入新课时：`[上下文: 用轻松愉快、引人入胜的语气]`
- 总结时：`[上下文: 用沉稳、肯定的语气]`
- 词语认读时：`[上下文: 吐字清晰，速度适中，注意每个字的发音]`

**用法示例：**
```
[上下文: 请用温暖亲切的语气来说]同学们，腊八粥不仅是一道美食，更承载着浓浓的亲情。[停顿: 1.5秒]让我们一起来感受这份温暖吧。
```

**注意：**
- 上下文操作符应放在需要调整语气的段落开头或特定语句之前，起到设定该段语音基调的作用。
- 不需要每句话都加，只在语气、情绪有明显变化或需要特别表现时使用。通常一页使用 1 个上下文操作符即可，特殊情况下（如页面内语气有明显转折）可使用 2 个。
- 上下文操作符和停顿操作符可以配合使用，共同塑造讲述的节奏和表现力。

---

## 输出格式

- 你必须输出**纯脚本文本**，可直接用于语音合成引擎。**不要使用 Markdown 格式**（不要用代码块包裹、不要加粗、不要用任何 Markdown 标记）。
- 脚本中嵌入的操作符使用 `[Key: Value]` 格式，原样保留在文本中。
- 脚本整体应自然流畅，操作符与讲述文字融为一体，读起来就像一位老师在娓娓道来。

---

## 生成前自检清单

在输出最终脚本之前，请逐项确认：

- **充实度**：脚本字数是否达到参考标准？页面上的每个元素是否都得到了充分展开？是否避免了"一句话带过"的问题？
- **忠实性**：内容是否完全忠实于当前页截图和文本？有没有编造截图中不存在的内容？
- **视觉呼应**：是否提及了截图中的图片、插图、按钮、标注等视觉元素？
- **上下文衔接**：是否自然承接了前文脚本？是否为下一页做了适当铺垫？
- **操作符**：停顿操作符是否分布合理、节奏恰当？上下文操作符是否放在了合适的位置且贴合页面教学功能？
- **格式**：操作符格式是否为 `[Key: Value]`？输出是否为纯文本、无 Markdown？
- **口语化**：语言是否口语化、适合朗读？是否避免了过于书面化的表达？
- **教学感**：是否体现了教师授课的口吻和引导感？

---

## 生成流程

请按以下步骤完成脚本生成：

1. **观察截图**：仔细查看当前页面的视觉布局，注意每一个可见元素——图片、文字、按钮、图标、颜色标注等，在脑海中形成对页面的完整印象。
2. **阅读文本**：阅读当前页文本提取内容和整份课件文本，明确当前页在课件流程中的位置和教学作用。
3. **回顾前文**：参考之前页面已生成的脚本，确定当前页的衔接点和过渡方式。
4. **构思脚本**：根据页面内容构思讲述结构——先讲什么、再讲什么、如何展开每个要点、如何收束并过渡。确保每个元素都得到充分展开，脚本长度达标。
5. **嵌入操作符**：在脚本中适当位置加入停顿操作符控制节奏，在需要调整语气的段落开头加入上下文操作符。
6. **逐项自检**：对照上述自检清单逐一确认。
7. **输出脚本**：直接输出当前页面的最终脚本，不要附加解释。
""";
}

/// <summary>
/// 课件页面材料信息，包括页面截图和页面内容文本
/// </summary>
/// <param name="SlideThumbnailFilePath"></param>
/// <param name="ContentText"></param>
public record CoursewareSlideMaterialInfo(FileInfo SlideThumbnailFilePath, string ContentText);

public record CoursewareMaterialInfo(List<CoursewareSlideMaterialInfo> SlideMaterialInfoList);