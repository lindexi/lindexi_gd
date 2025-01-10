using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Attributes;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Events;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core;

partial class TextEditorCore
{
    #region 公开事件

    /// <summary>
    /// 文档开始变更事件
    /// </summary>
    /// 内部使用 <see cref="LightTextEditorPlus.Core.Document.DocumentManager.InternalDocumentChanging"/> 事件
    [TextEditorPublicAPI]
    public event EventHandler? DocumentChanging;

    /// <summary>
    /// 文档变更完成事件
    /// </summary>
    /// 内部使用 <see cref="LightTextEditorPlus.Core.Document.DocumentManager.InternalDocumentChanged"/> 事件
    [TextEditorPublicAPI]
    public event EventHandler? DocumentChanged;

    /// <summary>
    /// 文档布局完成事件
    /// </summary>
    public event EventHandler<LayoutCompletedEventArgs>? LayoutCompleted;

    // todo 考虑 DocumentLayoutBoundsChanged 事件

    #region 光标

    /// <summary>
    /// 当前光标开始变更事件
    /// </summary>
    public event EventHandler<TextEditorValueChangeEventArgs<CaretOffset>>?
        CurrentCaretOffsetChanging;

    /// <summary>
    /// 当前光标已变更事件
    /// </summary>
    public event EventHandler<TextEditorValueChangeEventArgs<CaretOffset>>?
        CurrentCaretOffsetChanged;

    /// <summary>
    /// 当前选择范围开始变更事件
    /// </summary>
    public event EventHandler<TextEditorValueChangeEventArgs<Selection>>? CurrentSelectionChanging;

    /// <summary>
    /// 当前选择范围已变更事件。当光标变更或选择范围变更时，会触发此事件。即 <see cref="CurrentCaretOffsetChanged"/> 触发时，一定会随后触发此事件
    /// </summary>
    public event EventHandler<TextEditorValueChangeEventArgs<Selection>>? CurrentSelectionChanged;

    #endregion

    /// <summary>
    /// 布局变更后触发的事件
    /// </summary>
    public event EventHandler<TextEditorValueChangeEventArgs<ArrangingType>>? ArrangingTypeChanged;

    #endregion
}
