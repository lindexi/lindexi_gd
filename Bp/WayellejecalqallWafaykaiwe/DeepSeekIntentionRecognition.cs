namespace WayellejecalqallWafaykaiwe;

/// <summary>
/// 意图识别
/// </summary>
public class DeepSeekIntentionRecognition
{
    public const string 生成随机抽选 = "随机抽选";
    public const string 生成教学设计 = "生成教学设计";
    public const string 生成教案 = "生成教案";
    public const string 禁用敏感词过滤 = "禁用敏感词过滤";
    public const string 生成图片 = "生成图片";
    public const string 询问问题 = "询问问题";
    public const string 教学相关问题 = "教学相关问题";
    public const string 写报告 = "写报告";
    public const string 写材料 = "写材料";
    public const string 仿写内容 = "仿写内容";
    public const string 聊天 = "聊天";
    public const string 润色当前选中的文本 = "润色当前选中的文本";
    public const string 润色文本 = "润色文本";
    public const string 总结文本内容 = "总结文本";

    public async Task<IntentionRecognitionResult> RecognizeAsync(string text)
    {
        var intentionList = new string[]
        {
            询问问题,
            聊天,
            教学相关问题,
            写报告,
            写材料,
            仿写内容,
            生成随机抽选,
            生成教学设计,
            生成教案,
            禁用敏感词过滤,
            生成图片,
            润色当前选中的文本,
            润色文本,
            总结文本内容,
        };

        var deepSeekProvider = DeepSeekProvider.GetDeepSeekProvider();
        var userPrompt = text;
        //var prompt = $"""
        //              <|system|>你是一个意图识别机器人，可以识别出用户说话的意图是什么。请根据用户输入的内容，将用户输入的内容分类为以下类别中的一类。如果用户输入的内容无法进行归类，则归类为未知。请只对用户输入内容的意图进行归类，不要回答任何问题。只能使用以下意图类别，不能添加新的意图类别
        //              意图类别：
        //              {string.Join("\r\n", intentionList)}
        //              <|end|>
        //              <|user|>帮忙生成本份课件的教学设计内容，要求语句通顺<|end|>
        //              <|assistant|>生成教学设计<|end|>
        //              <|user|>我遇见一只小狗<|end|>
        //              <|assistant|>聊天<|end|>
        //              <|user|>如何指导小学低段学生进行写作<|end|>
        //              <|assistant|>询问问题<|end|>
        //              <|user|>{userPrompt}<|end|>
        //              <|assistant|>
        //              """;

        // 我正在给 GPT 编写提示词 Prompt 内容，请帮我将以下的提示词进行优化和翻译为英文：
        // 你是一个意图识别机器人，可以识别出用户说话的意图是什么。请根据用户输入的内容，将用户输入的内容分类为以下类别中的一类。如果用户输入的内容无法进行归类，则归类为未知。请只对用户输入内容的意图进行归类，不要回答任何问题。只能使用以下意图类别，不能添加新的意图类别

        var prompt = $"""
                      <|system|>You are an intent recognition bot capable of identifying the user's intent based on their input. Please classify the user's input into one of the following categories. If the user's input cannot be classified, categorize it as "Unknown". Only classify the intent of the user's input and do not answer any questions. Use only the provided intent categories and do not add any new ones.
                      Intent list：
                      {string.Join("\r\n", intentionList)}
                      <|end|>
                      <|user|>帮忙生成本份课件的教学设计内容，要求语句通顺<|end|>
                      <|assistant|>{生成教学设计}<|end|>
                      <|user|>我遇见一只小狗<|end|>
                      <|assistant|>{聊天}<|end|>
                      <|user|>根据课件信息，帮我生成一份教案<|end|>
                      <|assistant|>{生成教案}<|end|>
                      <|user|>如何指导小学低段学生进行写作<|end|>
                      <|assistant|>{询问问题}<|end|>
                      <|user|>{userPrompt}<|end|>
                      <|assistant|>
                      """;

        //prompt = $"""
        //          <|system|>
        //          You are an entity designed to detect the intent behind user inputs. Based on the user's input, you must identify the user's intent from the provided list. Respond solely with the intent from the list, without asking any further questions or including additional information. If the user's intent is not listed, please respond with "Other".
        //          Intent list:
        //          {string.Join("\r\n", intentionList)}
        //          <|end|>
        //          <|user|>{userPrompt}<|end|>
        //          <|assistant|>
        //          """;

        await using var response = await deepSeekProvider.ChatAsync(prompt);

        if (response.Success)
        {
            var responseText = await response.ReadAsResponseText();
            // 先读掉 think 的内容
            var (think, responseContent) = DeepSeekContentParser.Parse(responseText);

            var intention = responseContent.Trim();

            if (intention.StartsWith("<|assistant|>"))
            {
                intention = intention.Substring("<|assistant|>".Length);
            }
            if (intention.EndsWith("<|end|>"))
            {
                intention = intention.Substring(0, intention.Length - "<|end|>".Length);
            }

            var success = intentionList.Contains(intention);
            return new IntentionRecognitionResult
            {
                Success = success,
                Intention = intention
            };
        }
        else
        {
            return new IntentionRecognitionResult
            {
                Success = false,
                Intention = "未知"
            };
        }
    }
}

public record DeepSeekContent(string Think, string Response);

public static class DeepSeekContentParser
{
    public static DeepSeekContent Parse(string content)
    {
        // 格式内容是：
        // <think> 
        // 思考内容</think>
        // 正文内容
        // 先读掉 think 的内容
        var thinkStart = content.IndexOf("<think>", StringComparison.Ordinal);
        var thinkEnd = content.IndexOf("</think>", StringComparison.Ordinal);
        if (thinkStart >= 0 && thinkEnd > thinkStart)
        {
            var think = content.Substring(thinkStart + "<think>".Length, thinkEnd - thinkStart - "<think>".Length);
            var response = content.Substring(thinkEnd + "</think>".Length);
            return new DeepSeekContent(think, response);
        }
        else
        {
            return new DeepSeekContent(string.Empty, content);
        }
    }
}

public struct IntentionRecognitionResult
{
    public bool Success { get; init; }

    /// <summary>
    /// 意图
    /// </summary>
    public string Intention { get; init; }
}