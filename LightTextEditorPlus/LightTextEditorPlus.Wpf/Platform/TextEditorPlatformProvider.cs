using System;
using System.Diagnostics;
using System.Windows.Threading;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.UndoRedo;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Utils.Threading;

namespace LightTextEditorPlus;

internal class TextEditorPlatformProvider : PlatformProvider
{
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

    public override ITextLogger? BuildTextLogger()
    {
        return TextEditor.BuildCustomTextLogger() ?? base.BuildTextLogger();
    }

    #endregion

    private void UpdateLayout()
    {
        Debug.Assert(_lastTextLayout is not null);
        if (_lastTextLayout is {} lastTextLayout)
        {
            _lastTextLayout = null;
            lastTextLayout();
        }
    }

    private TextEditor TextEditor { get; }
    private readonly DispatcherRequiring _textLayoutDispatcherRequiring;
    private Action? _lastTextLayout;

    public override void RequireDispatchUpdateLayout(Action updateLayoutAction)
    {
        _lastTextLayout = updateLayoutAction;
        _textLayoutDispatcherRequiring.Require();
    }

    public override void InvokeDispatchUpdateLayout(Action updateLayoutAction)
    {
        _lastTextLayout = updateLayoutAction;
        _textLayoutDispatcherRequiring.Invoke(withRequire: true);
    }

    /// <summary>
    /// 尝试执行布局，如果无需布局，那就啥都不做
    /// </summary>
    public bool EnsureLayoutUpdated()
    {
        if (_lastTextLayout is null)
        {
            // 没有需要更新的布局
            return false;
        }

        _textLayoutDispatcherRequiring.Invoke();
        return true;
    }

    public override ICharInfoMeasurer? GetCharInfoMeasurer()
    {
        return _charInfoMeasurer;
    }

    private readonly CharInfoMeasurer _charInfoMeasurer;

    public override IRenderManager? GetRenderManager()
    {
        return TextEditor;
    }

    public override IPlatformRunPropertyCreator GetPlatformRunPropertyCreator() => _runPropertyCreator;

    private readonly RunPropertyCreator _runPropertyCreator; //= new RunPropertyCreator();

    public override double GetFontLineSpacing(IReadOnlyRunProperty runProperty)
    {
        return runProperty.AsRunProperty().GetRenderingFontFamily().LineSpacing;
    }

    public override IPlatformFontNameManager GetPlatformFontNameManager()
    {
        return TextEditor.StaticConfiguration.PlatformFontNameManager;
    }
}