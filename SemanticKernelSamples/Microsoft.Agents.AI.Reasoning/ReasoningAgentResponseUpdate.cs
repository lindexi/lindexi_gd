namespace Microsoft.Agents.AI.Reasoning;

public class ReasoningAgentResponseUpdate : AgentResponseUpdate
{
    public ReasoningAgentResponseUpdate(AgentResponseUpdate origin) : base(origin.Role, origin.Contents)
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

    public AgentResponseUpdate Origin { get; }

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
    /// 是否重新进入内容输出。
    /// 区别于 <see cref="IsFirstOutputContent"/>，该属性表示已经输出过内容后，在后续交错流中再次回到内容输出。
    /// </summary>
    public bool IsReenterOutputContent { get; set; }

    /// <summary>
    /// 思考的首次输出
    /// </summary>
    public bool IsFirstThinking { get; set; }

    /// <summary>
    /// 是否重新进入思考。
    /// 区别于 <see cref="IsFirstThinking"/>，该属性表示已经输出过内容后，在后续交错流中再次回到思考阶段。
    /// </summary>
    public bool IsReenterThinking { get; set; }

    /// <summary>
    /// 是否思考结束
    /// </summary>
    public bool IsThinkingEnd { get; set; }
}