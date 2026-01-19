namespace Microsoft.Agents.AI.Reasoning;

public class ReasoningAgentRunResponseUpdate : AgentRunResponseUpdate
{
    public ReasoningAgentRunResponseUpdate(AgentRunResponseUpdate origin) : base(origin.Role, origin.Contents)
    {
        Origin = origin;
        AdditionalProperties = origin.AdditionalProperties;
        AuthorName = origin.AuthorName;
        CreatedAt = origin.CreatedAt;
        MessageId = origin.MessageId;
        RawRepresentation = origin.RawRepresentation;
        ResponseId = origin.ResponseId;
        ContinuationToken = origin.ContinuationToken;
        AgentId = origin.AgentId;
    }

    public AgentRunResponseUpdate Origin { get; }

    public string? Reasoning { get; set; }

    /// <summary>
    /// 是否首次输出内容，前面输出的都是内容
    /// </summary>
    /// 仅内容输出，无思考的首次内容输出：
    /// - IsFirstOutputContent = true
    /// - IsFirstThinking = false
    /// - IsThinkingEnd = false
    /// 有思考，完成思考后的首次内容输出：
    /// - IsFirstOutputContent = true
    /// - IsFirstThinking = false
    /// - IsThinkingEnd = true
    public bool IsFirstOutputContent { get; set; }

    /// <summary>
    /// 思考的首次输出
    /// </summary>
    public bool IsFirstThinking { get; set; }

    /// <summary>
    /// 是否思考结束
    /// </summary>
    public bool IsThinkingEnd { get; set; }
}