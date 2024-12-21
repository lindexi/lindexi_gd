using System;
using LightTextEditorPlus.Core.Utils;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 表示一个换行的字符
/// </summary>
public class LineBreakCharObject : ICharObject
{
    /// <summary>
    /// 获取换行字符实例
    /// </summary>
    public static LineBreakCharObject Instance { get; } = new LineBreakCharObject();

    private LineBreakCharObject()
    {
    }

    bool IEquatable<string>.Equals(string? other)
    {
        if (string.Equals(other, "\r\n"))
        {
            return true;
        }
        return false;
    }

    ICharObject IDeepCloneable<ICharObject>.DeepClone()
    {
        return this;
    }

    string ICharObject.ToText() => TextContext.NewLine;

    /// <inheritdoc />
    public override string ToString() => ((ICharObject)this).ToText();
}