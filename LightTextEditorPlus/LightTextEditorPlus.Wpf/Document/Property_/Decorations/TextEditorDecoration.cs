using System;
using System.Windows;

namespace LightTextEditorPlus.Document.Decorations;

/// <summary>
/// 文本的装饰
/// </summary>
public abstract class TextEditorDecoration
{
    /// <summary>
    /// 文本的装饰
    /// </summary>
    protected TextEditorDecoration(TextDecorationLocation textDecorationLocation)
    {
        TextDecorationLocation = textDecorationLocation;
    }

    /// <summary>
    /// 获取文本的装饰放在文本的哪里
    /// </summary>
    public TextDecorationLocation TextDecorationLocation { get; }

    /// <summary>
    /// 创建装饰
    /// </summary>
    /// <returns></returns>
    public abstract BuildDecorationResult BuildDecoration(in BuildDecorationArgument argument);

    /// <summary>
    /// 从此装饰层中的视角认为两个 RunProperty 是否是相同的
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public virtual bool AreSameRunProperty(RunProperty a, RunProperty b)
    {
        return a.Equals(b);
    }

    /// <summary>
    /// 判断两个 RunProperty 是否是相同的，通过判断是否包含了当前装饰层的装饰来判断是否相同
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    protected bool CheckSameRunPropertyByContainsCurrentDecoration(RunProperty a, RunProperty b)
    {
        var aContains = a.DecorationCollection.Contains(this);
        var bContains = b.DecorationCollection.Contains(this);
        return aContains && bContains; // 为什么取都包含？因为首次判定，必然是包含当前的装饰，否则也就不会进来了
    }

    /// <summary>
    /// 隐式转换
    /// </summary>
    public static implicit operator TextEditorDecoration(TextDecoration textDecoration)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        // 相等判断是通过是否相同的类型进行判断的
        if (ReferenceEquals(obj, this))
        {
            return true;
        }

        if (obj is not TextEditorDecoration other)
        {
            return false;
        }

        return other.GetType() == this.GetType() && TextDecorationLocation == other.TextDecorationLocation;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(TextDecorationLocation, GetType());
    }
}