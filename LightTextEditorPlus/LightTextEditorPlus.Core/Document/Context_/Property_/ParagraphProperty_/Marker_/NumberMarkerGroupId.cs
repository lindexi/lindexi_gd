using System;
using System.Diagnostics.CodeAnalysis;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 自动编号组 ID
/// </summary>
public readonly struct NumberMarkerGroupId : IEquatable<NumberMarkerGroupId>
{
    /// <summary>
    /// 自动编号组 ID
    /// </summary>
    public NumberMarkerGroupId()
    {
        GlobalIdCount++;
        Id = GlobalIdCount;
    }

    private int Id { get; }

    private static int GlobalIdCount { get; set; }

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is NumberMarkerGroupId other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Id;
    }

    /// <inheritdoc />
    public bool Equals(NumberMarkerGroupId other)
    {
        return Id == other.Id;
    }
}