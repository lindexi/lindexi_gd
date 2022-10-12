using TextEditor = LightTextEditorPlus.Core.TextEditorCore;

namespace LightTextEditorPlus.Core.Layout;

class LayoutManager
{
    public LayoutManager(TextEditor textEditor)
    {
        TextEditor = textEditor;
        ArrangingLayoutProvider = new HorizontalArrangingLayoutProvider(textEditor);
    }

    public TextEditorCore TextEditor { get; }

    public void UpdateLayout()
    {
        // todo 完成测量最大宽度

        var sizeToContent = TextEditor.SizeToContent;

    }

    public ArrangingLayoutProvider ArrangingLayoutProvider { get; }
}

/// <summary>
/// 水平方向布局的提供器
/// </summary>
class HorizontalArrangingLayoutProvider : ArrangingLayoutProvider
{
    public HorizontalArrangingLayoutProvider(TextEditorCore textEditor) : base(textEditor)
    {
    }
}

/// <summary>
/// 实际的布局提供器
/// </summary>
abstract class ArrangingLayoutProvider
{
    protected ArrangingLayoutProvider(TextEditorCore textEditor)
    {
        TextEditor = textEditor;
    }

    public TextEditorCore TextEditor { get; }

}