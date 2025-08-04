using Avalonia.Controls;

namespace SimpleWrite.Views;

public partial class SimpleWriteMainView : UserControl
{
    public SimpleWriteMainView()
    {
        InitializeComponent();

        TextEditorInfo.SetTextEditorInfo(this, new TextEditorInfo(MainEditorView));
    }
}