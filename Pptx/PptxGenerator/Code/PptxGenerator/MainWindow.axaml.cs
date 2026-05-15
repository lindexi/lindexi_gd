using Avalonia.Controls;

namespace PptxGenerator;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Width = 1580;
        Height = 980;
        MinWidth = 1280;
        MinHeight = 800;
    }
}