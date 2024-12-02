using System;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 表示一个存储于文档树中的字符。存放的是人类语言的字符。例如表情是两个 char 表示的字符，即将此两个 char 放入一个对象中
/// </summary>
public interface ICharObject : IEquatable<string>, IDeepCloneable<ICharObject>
{
    /// <summary>
    /// 将字符转换成文本以便构建文档树
    /// </summary>
    /// <returns></returns>
    string ToText();
}