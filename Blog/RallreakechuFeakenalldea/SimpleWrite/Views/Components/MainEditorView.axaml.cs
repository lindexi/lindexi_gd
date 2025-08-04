using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using LightTextEditorPlus;
using LightTextEditorPlus.Primitive;
using SkiaSharp;

namespace SimpleWrite.Views.Components;

public partial class MainEditorView : UserControl
{
    public MainEditorView()
    {
        InitializeComponent();
        var textEditor = new LightTextEditorPlus.TextEditor();
        textEditor.TextEditorCore.SetExitDebugMode();

        // 优先采用 SetForeground 设置颜色
        TextElement.SetForeground(textEditor, Brushes.Azure);
        textEditor.SetStyleTextRunProperty(runProperty => runProperty with
        {
            FontSize = 25,
            Foreground = new SolidColorSkiaTextBrush(SKColors.Azure)
        });
        TextEditorBorder.Child = textEditor;

        CurrentTextEditor = textEditor;
    }

    public TextEditor CurrentTextEditor { get; }
}