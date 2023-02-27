using System;
using LightTextEditorPlus.Core.Utils;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 表示一个换行的字符
/// </summary>
public class LineBreakCharObject : ICharObject
{
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
}