using Microsoft.VisualBasic;

namespace UnoDemo;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

    }

    private void Button_OnClick(object sender, RoutedEventArgs e)
    {
        var button = (Button) sender;
        button.Width += 100;
    }
}

public static class ColumnSharedSizeHelper
{
    // Copy From https://github.com/Qiu233/WinUISharedSizeGroup

    public static readonly DependencyProperty IsSharedSizeScopeProperty =
        DependencyProperty.RegisterAttached("IsSharedSizeScope", typeof(bool), typeof(UIElement), new PropertyMetadata(false));

    private static readonly DependencyProperty SharedSizeGroupProperty =
        DependencyProperty.RegisterAttached("SharedSizeGroup", typeof(string), typeof(UIElement), new PropertyMetadata(null));

    public static void SetIsSharedSizeScope(DependencyObject o, bool group) => o.SetValue(IsSharedSizeScopeProperty, group);
    public static bool GetIsSharedSizeScope(DependencyObject o) => (bool) o.GetValue(IsSharedSizeScopeProperty);

    public static void SetSharedSizeGroup(DependencyObject o, string group)
    {
        o.SetValue(SharedSizeGroupProperty, group);

        if (o is FrameworkElement framework)
        {
            framework.Loaded -= FrameworkOnLoaded;
            framework.Loaded += FrameworkOnLoaded;

            void FrameworkOnLoaded(object sender, RoutedEventArgs e)
            {
                TrySetSize(framework);

                framework.SizeChanged -= Framework_SizeChanged;
                framework.SizeChanged += Framework_SizeChanged;
            }
        }
    }

    private static void Framework_SizeChanged(object sender, SizeChangedEventArgs args)
    {
        if (sender is not FrameworkElement currentFrameworkElement)
        {
            return;
        }

        TrySetSize(currentFrameworkElement);
    }

    private static void TrySetSize(FrameworkElement currentFrameworkElement)
    {
        var sharedSizeGroup = GetSharedSizeGroup(currentFrameworkElement);

        if (string.IsNullOrEmpty(sharedSizeGroup))
        {
            return;
        }

        FrameworkElement p = currentFrameworkElement;
        while (!ColumnSharedSizeHelper.GetIsSharedSizeScope(p))
        {
            if (VisualTreeHelper.GetParent(p) is not FrameworkElement fe)
            {
                return;
            }
            else
            {
                p = fe;
            }
        }

        if (p == currentFrameworkElement)
        {
            return;
        }

        if (!ColumnSharedSizeHelper.GetIsSharedSizeScope(p) || p is not Grid grid)
        {
            return;
        }

        var group = p.GetValue(GroupsProperty) as Dictionary<string, ColumnSharedSizeGroup>;
        if (group == null)
        {
            group = new Dictionary<string, ColumnSharedSizeGroup>();
            p.SetValue(GroupsProperty, group);
        }

        if (!group.ContainsKey(sharedSizeGroup))
        {
            group[sharedSizeGroup] = new ColumnSharedSizeGroup(grid);
        }

        group[sharedSizeGroup].Update(currentFrameworkElement);
    }

    public static string GetSharedSizeGroup(DependencyObject o)
    {
        return (string) o.GetValue(SharedSizeGroupProperty);
    }

    public static readonly DependencyProperty GroupsProperty =
        DependencyProperty.RegisterAttached(nameof(ColumnSharedSizeGroup), typeof(Dictionary<string, ColumnSharedSizeGroup>), typeof(UIElement),
            new PropertyMetadata(null));

    class ColumnSharedSizeGroup
    {
        public ColumnSharedSizeGroup(Grid grid)
        {
            _grid = grid;
        }

        private readonly Grid _grid;

        public void Update(FrameworkElement currentFrameworkElement)
        {
            var grid = _grid;
            var value = (int) currentFrameworkElement.GetValue(Grid.ColumnProperty);

            var column = grid.ColumnDefinitions[value];
            if (!_columns.Contains(column))
            {
                _columns.Add(column);
            }
            var adjustments = new List<ColumnDefinition>();
            var width = currentFrameworkElement.ActualWidth + currentFrameworkElement.Margin.Left + currentFrameworkElement.Margin.Right;
            if (width > _columnSize)
            {
                _columnSize = width;
                adjustments.AddRange(_columns);
            }
            else
            {
                adjustments.Add(column);
            }

            foreach (var columnDefinition in adjustments)
            {
                columnDefinition.Width = new GridLength(_columnSize);
            }
        }

        private readonly List<ColumnDefinition> _columns = [];
        private double _columnSize = 0.0;
    }
}
