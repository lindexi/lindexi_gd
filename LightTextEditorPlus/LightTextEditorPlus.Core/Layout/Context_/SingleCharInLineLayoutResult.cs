using System.Collections.Generic;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 测量行内字符的结果
/// </summary>
/// <param name="TotalSize">这一行的布局尺寸</param>
/// <param name="TaskCount">使用了多少个字符元素，有一些是需要连带字符后面的标点符号字符一起，于是就需要获取到多个</param>
/// <param name="CharSizeList">todo 只有一个时，不需要列表</param>
public readonly record struct SingleCharInLineLayoutResult(int TaskCount, Size TotalSize,IReadOnlyList<Size> CharSizeList)
{
    /// <summary>
    /// 是否这一行可以加入字符。不可加入等于需要换行
    /// </summary>
    public bool CanTake => TaskCount > 0;

    /// <summary>
    /// 是否需要换行了。等同于这一行不可再加入字符
    /// </summary>
    public bool ShouldBreakLine => CanTake is false;
}