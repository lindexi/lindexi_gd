using System;
using System.Diagnostics.CodeAnalysis;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 只读的文本字符属性
/// </summary>
public interface IReadOnlyRunProperty : IEquatable<IReadOnlyRunProperty>
{
    /// <summary>
    /// 字体大小
    /// </summary>
    /// <remarks>
    /// 没有明确的属性，交给文本业务层。有些使用像素、有些使用磅。但文本布局里面，将会将其作为对应 UI 框架的单位进行处理。如果传入非 UI 框架的单位，将需要 UI 层自行进行转换
    /// </remarks>
    double FontSize { get; }

    /// <summary>
    /// 用户设置的字体名
    /// </summary>
    /// 非底层找不到字体而进行回滚的字体，而是用户设置的字体
    /// 
    /// 在 Word 里面，可以同时设置一个文本 Run 的中文使用一个字体，西文使用一个字体
    /// 虽然 Word 这么做看起来不错，但是也存在设计无解的问题，例如西文字体的行高比中文字体的高
    /// 此时用户在输入中文，输入法先输入的是拼音，使用西文字体，此时行高变更，接着用户完成打字
    /// 输入法修改输入为中文，使用中文字体，于是行高再次变更，可以看到行高就在跳动
    /// 大部分的中文字体都有带英文字符，那不如就依然是单个字体
    FontName FontName { get; }

    ///// <summary>
    ///// 尝试获取属性
    ///// </summary>
    ///// <param name="propertyName"></param>
    ///// <param name="value"></param>
    ///// <returns></returns>
    //bool TryGetProperty(string propertyName, [NotNullWhen(true)] out IImmutableRunPropertyValue? value);
}