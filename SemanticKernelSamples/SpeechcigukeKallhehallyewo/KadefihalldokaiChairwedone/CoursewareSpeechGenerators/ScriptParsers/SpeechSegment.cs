namespace KadefihalldokaiChairwedone.CoursewareSpeechGenerators.ScriptParsers;

/// <summary>
/// 讲话的段落
/// </summary>
/// <param name="Text">文本</param>
/// <param name="PauseAfter">停顿时间</param>
internal sealed record SpeechSegment(string Text, TimeSpan? PauseAfter);