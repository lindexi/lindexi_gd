#if !USE_SKIA || USE_AllInOne

using System;

using LightTextEditorPlus.Core.Attributes;
using LightTextEditorPlus.Core.Carets;
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
}
#endif
