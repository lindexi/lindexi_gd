using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace JedicairqaiWejobajaibiqe;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ObservableCollection<TextItem> _items = [];

    private static readonly string[] _randomWords =
    [
        "Lorem", "ipsum", "dolor", "sit", "amet", "consectetur",
        "adipiscing", "elit", "sed", "do", "eiusmod", "tempor",
        "incididunt", "ut", "labore", "et", "dolore", "magna",
        "aliqua", "enim", "ad", "minim", "veniam", "quis",
        "nostrud", "exercitation", "ullamco", "laboris", "nisi",
        "aliquip", "ex", "ea", "commodo", "consequat", "duis",
        "aute", "irure", "in", "reprehenderit", "voluptate",
        "velit", "esse", "cillum", "fugiat", "nulla", "pariatur",
        "excepteur", "sint", "occaecat", "cupidatat", "proident",
        "sunt", "culpa", "qui", "officia", "deserunt", "mollit",
        "anim", "id", "est", "laborum", "perspiciatis", "unde",
        "omnis", "iste", "natus", "error", "voluptatem", "accusantium",
        "doloremque", "laudantium", "totam", "rem", "aperiam",
        "eaque", "ipsa", "quae", "ab", "illo", "inventore",
        "veritatis", "quasi", "architecto", "beatae", "dicta",
        "explicabo", "nemo", "ipsam", "quia", "voluptas", "aspernatur",
        "aut", "odit", "fugit", "consequuntur", "magni", "dolores",
        "eos", "ratione", "sequi", "nesciunt", "neque", "porro",
        "quisquam", "nihil", "molestiae", "illum", "fugiat", "quo"
    ];

    public MainWindow()
    {
        InitializeComponent();
        ItemsControl.ItemsSource = _items;
    }

    private void AddItemButton_Click(object sender, RoutedEventArgs e)
    {
        _items.Add(new TextItem { Content = $"Item {_items.Count + 1}" });
    }

    private void AddContentButton_Click(object sender, RoutedEventArgs e)
    {
        if (_items.Count == 0)
        {
            return;
        }

        var lastItem = _items[^1];
        var sb = new StringBuilder(512);

        while (sb.Length < 300)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            sb.Append(_randomWords[Random.Shared.Next(_randomWords.Length)]);
        }

        lastItem.Content += (lastItem.Content.Length > 0 ? " " : "") + sb.ToString();
    }

    private void ScrollToEndButton_Click(object sender, RoutedEventArgs e)
    {
        FooScrollViewer.ScrollToEnd();
    }
}