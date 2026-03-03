using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CairwellalnalweYemkerekera;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        TextBlock outerBlock = new TextBlock();
        Hyperlink link = new Hyperlink();
        InlineUIContainer inlineContainer = new InlineUIContainer();
        ContentPresenter innerContentPresenter = new ContentPresenter();

        outerBlock.Inlines.Add(link);
        link.Inlines.Add(inlineContainer);
        inlineContainer.Child = innerContentPresenter;

        innerContentPresenter.Content = new TextBlock()
        {
            Text = "HyperLink outside"
        };
        MainStackPanel.Children.Add(outerBlock);
    }
}