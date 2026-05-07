namespace KadefihalldokaiChairwedone.CoursewareSpeechGenerators;

/// <summary>
/// 课件讲稿视频生成输入参数。
/// </summary>
/// <param name="SlideInfoList">页面脚本和截图列表。</param>
public record CoursewareSpeechInfo(IReadOnlyList<CoursewareSpeechSlideInfo> SlideInfoList);