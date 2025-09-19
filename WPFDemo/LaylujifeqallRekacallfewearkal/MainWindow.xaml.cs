using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
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

namespace LaylujifeqallRekacallfewearkal;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new ToolTipData();
    }

    private void FlowDocumentScrollViewer_Loaded(object sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            var type = typeof(TextBox).Assembly.GetType("MS.Internal.Documents.UndoManager");
            var method = type?.GetMethod("GetUndoManager", BindingFlags.NonPublic | BindingFlags.Static);

            var result = method.Invoke(null, new object[] { sender });
            if (result is not null)
            {
                // if we're in here, the bug is about to happen, we've collected an undo scope from the outer text box
                FieldInfo field = result.GetType().GetField("_scope", BindingFlags.NonPublic | BindingFlags.Instance);
                var fieldValue = field?.GetValue(result) as TextBox;
                // you'll notice that the scope is the same instance
                Debug.Assert(fieldValue == _textBox);
            }

            var toolTip = DataContext as ToolTipData;
            toolTip!.FlowDocument = new FlowDocument();
            var paragraph = new Paragraph(new Run("HelloToolTip2"));
            toolTip.FlowDocument.Blocks.Add(paragraph);

            toolTip.RaiseChanged(); // this will cause the crash as the flow document found the outer undoscope during the binding refresh
        });
    }
}

internal class ToolTipData : INotifyPropertyChanged
{
    public ToolTipData()
    {
        FlowDocument = new FlowDocument();
        var paragraph = new Paragraph(new Run("HelloToolTip"));
        FlowDocument.Blocks.Add(paragraph);
        PropertyChanged?.Invoke(this, new(nameof(FlowDocument)));
    }

    public FlowDocument FlowDocument { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RaiseChanged()
    {
        PropertyChanged?.Invoke(this, new(nameof(FlowDocument)));
    }
}