using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;

using SimpleWrite.ViewModels;

namespace SimpleWrite.Views;

public partial class SimpleWriteMainView : UserControl
{
    public SimpleWriteMainView()
    {
        InitializeComponent();

        TextEditorInfo.SetTextEditorInfo(this, new TextEditorInfo(MainEditorView));
    }

    public SimpleWriteMainViewModel ViewModel => (SimpleWriteMainViewModel) DataContext!;

    public async Task OpenFileAsync(FileInfo file)
    {

    }
}