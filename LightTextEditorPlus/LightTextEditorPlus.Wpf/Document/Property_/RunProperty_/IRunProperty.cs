using LightTextEditorPlus.Core.Document;
using System.Windows;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 文本字符属性
/// </summary>
/// 设计上文本字符属性是只读的，但是 IReadOnlyRunProperty 命名已用
/// 为什么需要再定义一个接口？因为文本的字符属性是用到了很多平台相关属性，有些定义是有些平台不支持的
/// 这个接口是用来框架外的业务层的，在框架内是不使用接口的
public interface IRunProperty : IReadOnlyRunProperty
{
    /// <summary>
    /// 前景色
    /// </summary>
    ImmutableBrush Foreground { get; }

    /// <summary>
    /// 背景色
    /// </summary>
    ImmutableBrush? Background { get; }

    /// <summary>
    /// 不透明度
    /// </summary>
    double Opacity { get; }

    FontStretch Stretch { get; }
    FontWeight FontWeight { get; }
    FontStyle FontStyle { get; }
}