namespace YikelnukairjurCelcerlurkeneka;

/// <summary>
/// 意图识别
/// </summary>
public class PhiIntentionRecognition
{
    public const string 生成随机抽选 = "生成随机抽选";
    public const string 生成教学设计 = "生成教学设计";
    public const string 修改聊天 = "修改聊天";
    public const string 禁用敏感词过滤 = "禁用敏感词过滤";
    public const string 生成图片 = "生成图片";
    public const string 询问问题 = "询问问题";
    public const string 教学相关问题 = "教学相关问题";
    public const string 写报告 = "写报告";
    public const string 写材料 = "写材料";
    public const string 聊天 = "聊天";

    public async Task<IntentionRecognitionResult> RecognizeAsync(string text)
    {
        var intentionList = new string[]
        {
            询问问题,
            聊天,
            教学相关问题,
            写报告,
            写材料,
            生成随机抽选,
            生成教学设计,
            修改聊天,
            禁用敏感词过滤,
            生成图片,
        };

        var phiProvider = PhiProvider.GetPhiProvider();
        var userPrompt = text;
        var prompt = $"""
                      <|system|>你是一个意图识别机器人，可以识别出用户说话的意图是什么。请根据用户输入的内容，将用户输入的内容分类为以下类别中的一类。如果用户输入的内容无法进行归类，则归类为未知。请只对用户输入内容的意图进行归类，不要回答任何问题。
                      意图类别：
                      {string.Join("\r\n", intentionList)}
                      <|end|>
                      <|user|>{userPrompt}<|end|>
                      <|assistant|>
                      """;

        prompt = $"""
                  <|system|>
                  You are an entity designed to detect the intent behind user inputs. Based on the user's input, you must identify the user's intent from the provided list. Respond solely with the intent from the list, without asking any further questions or including additional information. If the user's intent is not listed, please respond with "Other".
                  Intent list:
                  {string.Join("\r\n", intentionList)}
                  <|end|>
                  <|user|>{userPrompt}<|end|>
                  <|assistant|>
                  """;

        await using var response = await phiProvider.ChatAsync(prompt);

        if (response.Success)
        {
            var responseText = await response.ReadToEndAsync();
            var intention = responseText.Trim();
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

public struct IntentionRecognitionResult
{
    public bool Success { get; init; }

    /// <summary>
    /// 意图
    /// </summary>
    public string Intention { get; init; }
}