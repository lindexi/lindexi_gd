namespace KadefihalldokaiChairwedone.CoursewareSpeechGenerators;

/// <summary>
/// 脚本信息
/// </summary>
/// <param name="ContextText">上下文信息</param>
/// <param name="Segments">讲话的段落</param>
internal sealed record ScriptInput(string? ContextText, IReadOnlyList<SpeechSegment> Segments);