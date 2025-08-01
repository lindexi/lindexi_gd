#if !USE_SKIA || USE_AllInOne

using System;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Attributes;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Editing;
using LightTextEditorPlus.Core.Events;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;

namespace LightTextEditorPlus;

// 此文件存放状态获取相关的方法
[APIConstraint("TextEditor.Property.Shared.txt")]
partial class TextEditor
{
    #region 日志

    /// <summary>
    /// 日志
    /// </summary>
    public ITextLogger Logger
    {
        get => TextEditorCore.Logger;
        set => TextEditorCore.Logger = value;
    }

    #endregion

    #region 功能特性

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.Features"/>
    public TextFeatures Features
    {
        get => TextEditorCore.Features;
        set => TextEditorCore.Features = value;
    }

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.EnableFeatures"/>
    public void EnableFeatures(TextFeatures features) => TextEditorCore.EnableFeatures(features);

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.DisableFeatures"/>
    public void DisableFeatures(TextFeatures features) => TextEditorCore.DisableFeatures(features);

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.IsFeaturesEnable"/>
    public bool IsFeaturesEnable(TextFeatures features) => TextEditorCore.IsFeaturesEnable(features);

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.CheckFeaturesDisableWithLog"/>
    public bool CheckFeaturesDisableWithLog(TextFeatures features) => TextEditorCore.CheckFeaturesDisableWithLog(features);

    #endregion
}
#endif
