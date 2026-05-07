namespace KadefihalldokaiChairwedone.CoursewareSpeechGenerators;

/// <summary>
/// 课件讲稿的页面信息
/// </summary>
/// <param name="PlainScriptText">原始的脚本内容，格式如 [上下文: 用轻松亲切的语气，语速适中]
/// 同学们好，欢迎来到今天的六年级语文下册课堂。[停顿: 1秒]</param>
/// <param name="SlideThumbnailFile">页面截图内容</param>
public record CoursewareSpeechSlideInfo(string PlainScriptText, FileInfo SlideThumbnailFile);