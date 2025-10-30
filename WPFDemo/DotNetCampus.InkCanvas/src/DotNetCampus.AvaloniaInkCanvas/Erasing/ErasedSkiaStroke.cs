using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace DotNetCampus.Inking.Erasing;

/// <summary>
/// 橡皮擦掉之后的笔迹
/// </summary>
public readonly record struct ErasedSkiaStroke
{
    public ErasedSkiaStroke(SkiaStroke originStroke, IReadOnlyList<SkiaStroke>? newStrokeList, bool isErased)
    {
        OriginStroke = originStroke;
        NewStrokeList = newStrokeList;
        IsErased = isErased;

        Debug.Assert(isErased == (newStrokeList != null), "被擦掉的情况下，必定存在列表，即使是空列表");
    }

    /// <summary>
    /// 原始的笔迹
    /// </summary>
    public SkiaStroke OriginStroke { get; }

    /// <summary>
    /// 被擦掉之后的新笔迹列表，可能为空列表。空列表和 null 有区别，空列表表示被擦掉了，但是没有新的笔迹，而 null 表示没被擦掉
    /// </summary>
    public IReadOnlyList<SkiaStroke>? NewStrokeList { get; }

    /// <summary>
    /// 是否被擦掉了，即 <see cref="OriginStroke"/> 被擦掉成多条 <see cref="NewStrokeList"/> 新的笔迹
    /// </summary>
    [MemberNotNullWhen(true, nameof(NewStrokeList))]
    public bool IsErased { get; }
}