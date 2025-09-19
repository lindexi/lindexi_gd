using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.UndoRedo;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Editing;
using LightTextEditorPlus.Utils.Threading;

using System;
using System.Diagnostics;
using System.Windows.Threading;

namespace LightTextEditorPlus.Platform;

/// <summary>
/// 文本库的平台提供者
/// </summary>
public class TextEditorPlatformProvider : PlatformProvider
{
    /// <summary>
    /// 创建文本库的平台提供者
    /// </summary>
    /// <param name="textEditor"></param>
    public TextEditorPlatformProvider(TextEditor textEditor)
    {
        TextEditor = textEditor;

        _textLayoutDispatcherRequiring = new DispatcherRequiring(UpdateLayout, DispatcherPriority.Render);
        _charInfoMeasurer = new CharInfoMeasurer(textEditor);
        _runPropertyCreator = new RunPropertyCreator(textEditor);
    }

    #region 可基类重写方法

    /// <inheritdoc />
    /// 如果需要自定义撤销恢复，可以获取文本编辑器重写的方法
    /// 默认文本库是独立的撤销恢复，每个文本编辑器都有自己的撤销恢复。如果想要全局的撤销恢复，可以自定义一个全局的撤销恢复
    public override ITextEditorUndoRedoProvider BuildTextEditorUndoRedoProvider()
    {
        return TextEditor.BuildCustomTextEditorUndoRedoProvider() ?? base.BuildTextEditorUndoRedoProvider();
    }

    /// <inheritdoc />
    public override ITextLogger? BuildTextLogger()
    {
        return TextEditor.BuildCustomTextLogger() ?? base.BuildTextLogger();
    }

    #endregion

    private void UpdateLayout()
    {
        _updatingLayout = true;
        Debug.Assert(_layoutUpdateAction is not null);
        try
        {
            // Fixed : 这里有安全性问题。如果没有获取变量，则可能在 _layoutUpdateAction 执行过程中，调用进了 RequireDispatchUpdateLayout 或 InvokeDispatchUpdateLayout 方法，从而导致 _layoutUpdateAction 对象变更。即在进行禁止多次执行的 `_layoutUpdateAction = null` 方法时，将新的布局委托赋空
            // 立即将其赋空则是安全的
            Action? layoutUpdateAction = _layoutUpdateAction;

            // 禁止多次执行
            _layoutUpdateAction = null;

            layoutUpdateAction?.Invoke();
        }
        finally
        {
            _updatingLayout = false;
        }
    }
    private bool _updatingLayout;

    private TextEditor TextEditor { get; }
    private readonly DispatcherRequiring _textLayoutDispatcherRequiring;
    private Action? _layoutUpdateAction;

    /// <inheritdoc />
    public override void RequireDispatchUpdateLayout(Action updateLayoutAction)
    {
        _layoutUpdateAction = updateLayoutAction;
        _textLayoutDispatcherRequiring.Require();
    }

    /// <inheritdoc />
    public override void InvokeDispatchUpdateLayout(Action updateLayoutAction)
    {
        _layoutUpdateAction = updateLayoutAction;
        _textLayoutDispatcherRequiring.Invoke(withRequire: true);
    }

    /// <summary>
    /// 尝试执行布局，如果无需布局，那就啥都不做
    /// </summary>
    /// <returns>True: 有需要进行布局；False: 不需要进行布局</returns>
    public bool EnsureLayoutUpdated()
    {
        if (_updatingLayout)
        {
            throw new InvalidOperationException($"正在布局的过程中触发立刻布局，可能出现无限递归炸堆栈");
        }

        if (_layoutUpdateAction is null)
        {
            // 没有需要更新的布局
            Debug.Assert(!TextEditor.TextEditorCore.IsDirty || TextEditor.TextEditorCore.DocumentManager.CharCount == 0, "无需布局的情况只有两个，要么文本不是脏的 IsDirty 为 false 值。要么是空文本，即 CharCount 为零");
            return false;
        }

        _textLayoutDispatcherRequiring.Invoke();
        return true;
    }

    /// <inheritdoc />
    public override ICharInfoMeasurer? GetCharInfoMeasurer()
    {
        return _charInfoMeasurer;
    }

    private readonly CharInfoMeasurer _charInfoMeasurer;

    /// <inheritdoc />
    public override IRenderManager? GetRenderManager()
    {
        return TextEditor;
    }

    /// <inheritdoc />
    public override IPlatformRunPropertyCreator GetPlatformRunPropertyCreator() => _runPropertyCreator;

    private readonly RunPropertyCreator _runPropertyCreator; //= new RunPropertyCreator();

    /// <inheritdoc />
    public override double GetFontLineSpacing(IReadOnlyRunProperty runProperty)
    {
        return runProperty.AsRunProperty().GetRenderingFontFamily().LineSpacing;
    }

    /// <inheritdoc />
    public override IPlatformFontNameManager GetPlatformFontNameManager()
    {
        return TextEditor.StaticConfiguration.PlatformFontNameManager;
    }

    public virtual TextEditorHandler GetHandler()
    {
        return new TextEditorHandler(TextEditor);
    }
}