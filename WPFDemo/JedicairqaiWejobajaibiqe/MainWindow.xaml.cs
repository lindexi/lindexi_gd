using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JedicairqaiWejobajaibiqe;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ObservableCollection<TextItem> _itemsControlItems = [];
    private readonly ObservableCollection<TextItem> _listViewItems = [];
    private readonly ObservableCollection<TextItem> _listBoxItems = [];

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
        ItemsControl.ItemsSource = _itemsControlItems;
        ListView.ItemsSource = _listViewItems;
        ListBox.ItemsSource = _listBoxItems;
    }

    private static string GenerateRandomText()
    {
        var sb = new StringBuilder(512);

        while (sb.Length < 300)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            sb.Append(_randomWords[Random.Shared.Next(_randomWords.Length)]);
        }

        return sb.ToString();
    }

    private static void AddItem(ObservableCollection<TextItem> items)
    {
        items.Add(new TextItem { Content = $"Item {items.Count + 1}" });
    }

    private static void AddContent(ObservableCollection<TextItem> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        var lastItem = items[^1];
        var separator = lastItem.Content.Length > 0 ? " " : "";
        lastItem.Content += separator + GenerateRandomText();
    }

    // ========== ItemsControl ==========

    private void ItemsControl_AddItem_Click(object sender, RoutedEventArgs e)
    {
        AddItem(_itemsControlItems);
    }

    private void ItemsControl_AddContent_Click(object sender, RoutedEventArgs e)
    {
        AddContent(_itemsControlItems);
    }

    private void ItemsControl_ScrollToEnd_Click(object sender, RoutedEventArgs e)
    {
        ItemsControlScrollViewer.ScrollToEnd();
    }

    // ========== ListView ==========

    private void ListView_AddItem_Click(object sender, RoutedEventArgs e)
    {
        AddItem(_listViewItems);
    }

    private void ListView_AddContent_Click(object sender, RoutedEventArgs e)
    {
        AddContent(_listViewItems);
    }

    private void ListView_ScrollToEnd_Click(object sender, RoutedEventArgs e)
    {
        var scrollViewer = FindChild<ScrollViewer>(ListView);
        scrollViewer?.ScrollToEnd();
    }

    private T? FindChild<T>(FrameworkElement element) where T : FrameworkElement
    {
        foreach (var frameworkElement in LogicalTreeHelper.GetChildren(element).OfType<FrameworkElement>())
        {
            if (frameworkElement is T result)
            {
                return result;
            }

            var childResult = FindChild<T>(frameworkElement);
            if (childResult != null)
            {
                return childResult;
            }
        }

        var childrenCount = VisualTreeHelper.GetChildrenCount(element);
        for (int i = 0; i < childrenCount; i++)
        {
            var visualChild = VisualTreeHelper.GetChild(element,i);
            if (visualChild is T result)
            {
                return result;
            }

            if (visualChild is FrameworkElement visualFrameworkElement)
            {
                var childResult = FindChild<T>(visualFrameworkElement);
                if (childResult != null)
                {
                    return childResult;
                }
            }
        }

        return null;
    }

    // ========== ListBox ==========

    private void ListBox_AddItem_Click(object sender, RoutedEventArgs e)
    {
        AddItem(_listBoxItems);
    }

    private void ListBox_AddContent_Click(object sender, RoutedEventArgs e)
    {
        AddContent(_listBoxItems);
    }

    private void ListBox_ScrollToEnd_Click(object sender, RoutedEventArgs e)
    {
        var scrollViewer = FindChild<ScrollViewer>(ListBox);
        scrollViewer?.ScrollToEnd();
    }
}