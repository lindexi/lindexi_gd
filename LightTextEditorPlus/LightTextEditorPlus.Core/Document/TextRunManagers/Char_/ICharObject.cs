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