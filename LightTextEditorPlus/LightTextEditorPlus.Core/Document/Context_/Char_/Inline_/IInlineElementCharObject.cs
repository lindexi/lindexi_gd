using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 内联元素字符
/// </summary>
/// 参阅 `ICharObject 扩展类型命名决策文档.md` 文档
public interface IInlineElementCharObject : ICharObject
{
    /// <summary>
    /// 测量内联元素的尺寸，感知到布局
    /// </summary>
    /// <param name="runProperty">当前运行的属性</param>
    void Measure(IReadOnlyRunProperty runProperty)
    {

    }

    /// <summary>
    /// 布局内联元素。给定布局信息，如行剩余宽度和是否在行首
    /// </summary>
    /// <param name="lineRemainingWidth">行剩余宽度</param>
    /// <param name="isInLineStart">是否在行首</param>
    void Layout(double lineRemainingWidth, bool isInLineStart)
    {
        // 返回能否布局
        // 如果是在行首那就没得玩了呀

        // 布局的时候告知情况，比如行的高度之类。也许 BaseLineRatio 是不需要的，只是给定左上角坐标而已
        // 可能 Layout 这个词不好，而是 Arrange
        // 再有 Layout 给定左上角坐标，和行的高度这些信息
    }

    /// <summary>
    /// 内联元素的尺寸
    /// </summary>
    TextSize Size { get; }

    /// <summary>
    /// 基线所在的比例。比如高度为 10 ，基线比例为 0.7 ，那么将按照基线作为 7 3 划分。用于公式文本的对齐效果
    /// </summary>
    double BaseLineRatio { get; }
}