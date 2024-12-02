using System;
using System.Collections.Generic;

using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 测量行内字符的结果
/// </summary>
public readonly struct SingleCharInLineLayoutResult
{
    /// <summary>
    /// 测量行内字符的结果
    /// </summary>
    /// <param name="totalSize">这一行的布局尺寸</param>
    /// <param name="takeCount">使用了多少个字符元素，有一些是需要连带字符后面的标点符号字符一起，于是就需要获取到多个</param>
    public SingleCharInLineLayoutResult(int takeCount, Size totalSize)
    {
        TakeCount = takeCount;
        TotalSize = totalSize;

        //if (takeCount > 1 && (charSizeList == null || charSizeList.Count != takeCount))
        //{
        //    throw new ArgumentException($"当获取超过一个字符时，需要给 {nameof(charSizeList)} 赋值，且要求 {nameof(charSizeList)} 的元素数量等于所获取的字符数量");
        //}

        //CharSizeList = charSizeList;
    }

    /// <summary>
    /// 是否这一行可以加入字符。不可加入等于需要换行
    /// </summary>
    public bool CanTake => TakeCount > 0;

    /// <summary>
    /// 是否需要换行了。等同于这一行不可再加入字符
    /// </summary>
    public bool ShouldBreakLine => CanTake is false;

    /// <summary>使用了多少个字符元素，有一些是需要连带字符后面的标点符号字符一起，于是就需要获取到多个</summary>
    public int TakeCount { get; init; }

    /// <summary>所有的字符合起来的布局尺寸</summary>
    public Size TotalSize { get; init; }

    ///// <summary>当 <see cref="TakeCount"/> 大于一个时，将存放每个字符的尺寸</summary>
    //public IReadOnlyList<LineCharSize>? CharSizeList { get; init; }

    //public void Deconstruct(out int TaskCount, out LineCharSize TotalSize, out IReadOnlyList<LineCharSize>? CharSizeList)
    //{
    //    TaskCount = this.TaskCount;
    //    TotalSize = this.TotalSize;
    //    CharSizeList = this.CharSizeList;
    //}

    /// <inheritdoc />
    public override string ToString()
        => $"CanTake={CanTake};ShouldBreakLine={ShouldBreakLine};TakeCount={TakeCount};TotalSize={TotalSize}";
}