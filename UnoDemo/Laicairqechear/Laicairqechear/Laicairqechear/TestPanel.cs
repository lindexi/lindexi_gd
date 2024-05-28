using Windows.Foundation;
using Microsoft.UI;

namespace Laicairqechear;

class TestPanel : Panel
{
    public TestPanel()
    {
        var grid = new Grid()
        {
            Background = new SolidColorBrush(Colors.Black),
            Width = 500,
            Height = 500,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            RowDefinitions =
            {
                new RowDefinition(),
                new RowDefinition(),
                new RowDefinition(),
                new RowDefinition(),
            },
            ColumnDefinitions =
            {
                new ColumnDefinition(),
                new ColumnDefinition(),
                new ColumnDefinition(),
                new ColumnDefinition(),
            },
            Children =
            {
                new Border()
                {
                    Background = new SolidColorBrush(Colors.Red),
                    Margin = new Thickness(2)
                }.GridRow(0).GridColumn(0),
                new Border()
                {
                    Background = new SolidColorBrush(Colors.Green),
                    Margin = new Thickness(2)
                }.GridRow(0).GridColumn(1),
                new Border()
                {
                    Background = new SolidColorBrush(Colors.Blue),
                    Margin = new Thickness(2)
                }.GridRow(0).GridColumn(2),
                new Border()
                {
                    Background = new SolidColorBrush(Colors.Yellow),
                    Margin = new Thickness(2)
                }.GridRow(0).GridColumn(3),
                new Border()
                {
                    Background = new SolidColorBrush(Colors.Red),
                    Margin = new Thickness(2)
                }.GridRow(1).GridColumn(0),
                new Border()
                {
                    Background = new SolidColorBrush(Colors.Green),
                    Margin = new Thickness(2)
                }.GridRow(1).GridColumn(3),
                new Border()
                {
                    Background = new SolidColorBrush(Colors.Blue),
                    Margin = new Thickness(2)
                }.GridRow(2).GridColumn(0),
                new Border()
                {
                    Background = new SolidColorBrush(Colors.Yellow),
                    Margin = new Thickness(2)
                }.GridRow(2).GridColumn(3),
                new Border()
                {
                    Background = new SolidColorBrush(Colors.Red),
                    Margin = new Thickness(2)
                }.GridRow(3).GridColumn(0),
                new Border()
                {
                    Background = new SolidColorBrush(Colors.Green),
                    Margin = new Thickness(2)
                }.GridRow(3).GridColumn(1),
                new Border()
                {
                    Background = new SolidColorBrush(Colors.Blue),
                    Margin = new Thickness(2)
                }.GridRow(3).GridColumn(2),
                new Border()
                {
                    Background = new SolidColorBrush(Colors.Yellow),
                    Margin = new Thickness(2)
                }.GridRow(3).GridColumn(3),
            }
        };
        
        Children.Add(grid);

        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return availableSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Children[0].Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));

        return finalSize;
    }
}
