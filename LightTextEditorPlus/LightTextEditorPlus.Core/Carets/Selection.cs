using System;
using System.Diagnostics;

namespace LightTextEditorPlus.Core.Carets;

/// <summary>
/// 表示选择范围，采用的是光标坐标系
/// </summary>
public readonly struct Selection : IEquatable<Selection>
{
    /// <summary>
    /// 创建选择范围
    /// </summary>
    /// <param name="startOffset"></param>
    /// <param name="length"></param>
    [DebuggerStepThrough]
    public Selection(CaretOffset startOffset, int length)
    {
        StartOffset = startOffset;
        EndOffset = new CaretOffset(startOffset.Offset + length);

        FrontOffset = startOffset;
        BehindOffset = EndOffset;
    }

    /// <summary>
    /// 通过两个光标偏移量构造一个选择段
    /// </summary>
    /// <param name="startOffset"></param>
    /// <param name="endOffset"></param>
    [DebuggerStepThrough]
    public Selection(CaretOffset startOffset, CaretOffset endOffset)
    {
        StartOffset = startOffset;
        EndOffset = endOffset;

        if (StartOffset.Offset < EndOffset.Offset)
        {
            FrontOffset = StartOffset;
            BehindOffset = EndOffset;
        }
        else
        {
            FrontOffset = EndOffset;
            BehindOffset = StartOffset;
        }
    }

    /// <summary>
    /// 获取当前<see cref="Selection"/>的长度
    /// </summary>
    public bool IsEmpty => Length == 0;

    /// <summary>
    /// 获取当前<see cref="Selection"/>的开始位置偏移量
    /// </summary>
    /// 开始选择和 <see cref="FrontOffset"/> 的不同在于，选择是支持方向是反的选择
    public CaretOffset StartOffset { get; }

    /// <summary>
    /// 获取当前选择的长度
    /// </summary>
    public int Length => BehindOffset.Offset - FrontOffset.Offset;

    /// <summary>
    /// 获取当前<see cref="Selection"/>更靠近文档开始位置的偏移量
    /// </summary>
    public CaretOffset FrontOffset { get; }

    /// <summary>
    /// 获取当前<see cref="Selection"/>更远离文档开始位置的偏移量
    /// </summary>
    public CaretOffset BehindOffset { get; }

    /// <summary>
    /// 获取当前<see cref="Selection"/>的结束位置偏移量
    /// </summary>
    public CaretOffset EndOffset { get; }

    /// <summary>
    /// 传入给定的 <paramref name="caretOffset"/> 判断是否落在选择范围内
    /// </summary>
    /// <param name="caretOffset"></param>
    /// <returns></returns>
    /// 也许这个命名不对
    public bool Contains(in CaretOffset caretOffset)
    {
        return FrontOffset.Offset <= caretOffset.Offset &&
               BehindOffset.Offset >= caretOffset.Offset;
    }

    /// <summary>
    /// 选择范围是否相同，无视起始点和结束点是否相反
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool EqualRange(Selection other)
    {
        return other.FrontOffset.Equals(FrontOffset) && other.BehindOffset.Equals(BehindOffset);
    }

    /// <inheritdoc />
    public override string ToString() => $"{FrontOffset.Offset}-{BehindOffset.Offset};Length={Length}";

    /// <inheritdoc />
    public bool Equals(Selection other)
    {
        return StartOffset.Equals(other.StartOffset) && FrontOffset.Equals(other.FrontOffset);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Selection other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(StartOffset, FrontOffset);
    }
}