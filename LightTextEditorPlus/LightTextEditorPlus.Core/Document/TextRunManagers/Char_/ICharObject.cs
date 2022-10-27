using System;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 表示一个存储于文档树中的字符
/// </summary>
public interface ICharObject : IEquatable<string>, IDeepCloneable<ICharObject>
{
    /// <summary>
    /// 将字符转换成文本以便构建文档树
    /// </summary>
    /// <returns></returns>
    string ToText();
}

/// <summary>
/// 表示一个由单个字符构成的字符
/// </summary>
/// 由于文本里面存在一些语言是不能使用一个 char 表示一个字符的。所以大部分情况下都是需要使用 string 表示
public interface ISingleCharObject : ICharObject
{
    char GetChar();
}