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

namespace WoleejallceWhallbelacherefallno;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
/// https://github.com/dotnet/wpf/issues/10032
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        InitializeTextBox();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
        => ReplaceCurrentTextWithItem();

    private void ReplaceCurrentTextWithItem()
    {
        if (!GetCurrentText(out TextPointer pos, out string text))
            return;

        // This line seems to be the trigger point for the error.
        pos.DeleteTextInRun(-text.Length);

        TextBoxInlines.Add(BuildItem(text));
    }

    private bool GetCurrentText(out TextPointer pos, out string beforeText)
    {
        pos = ItemsTextBox.CaretPosition;
        if (pos == null)
        {
            beforeText = null;
            return false;
        }

        beforeText = pos.GetTextInRun(LogicalDirection.Backward) ?? "";
        return true;
    }

    #region Helpers

    private InlineCollection TextBoxInlines { get; set; }

    private void InitializeTextBox()
    {
        var paragraph = new Paragraph();
        ItemsTextBox.Document.Blocks.Add(paragraph);

        TextBoxInlines = paragraph.Inlines;
        TextBoxInlines.Add(BuildItem("A really long line that takes most of the space"));
    }

    private InlineUIContainer BuildItem(string text)
        => new InlineUIContainer(new Label
        {
            Content = text,
            Background = Brushes.Yellow,
            Margin = new Thickness(0, 0, 6, 0)
        });

    #endregion
}
