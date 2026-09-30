using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace XiaoXiIme.ImeUi.Avalonia;

public sealed partial class CandidateWindow : Window
{
    public CandidateWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
