namespace AgentLib.Model;

/// <summary>
/// 会话标题的来源。
/// </summary>
public enum TitleSource
{
    /// <summary>默认标题「新会话」，尚未被任何逻辑覆盖。</summary>
    Default,

    /// <summary>由首条用户消息截断生成。</summary>
    AutoTruncated,

    /// <summary>由 LLM 生成。</summary>
    Generated,

    /// <summary>由用户显式设置。</summary>
    UserSet,
}
