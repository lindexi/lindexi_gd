using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CehaynilaiWhihelhearcahaihi;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        var i = 0;
        foreach (var file in Directory.GetFiles(Environment.CurrentDirectory, "*.png"))
        {
            var bitmapImage = new BitmapImage(new Uri(file));
            ModelCollection.Add(new Model(bitmapImage, i));

            i++;
        }
        
        InitializeComponent();
    }

    public ObservableCollection<Model> ModelCollection { get; } = new ObservableCollection<Model>();

    private void Bd_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is not Border border)
        {
            return;
        }

        if (e.WidthChanged)
        {
            border.Height = e.NewSize.Width * 9 / 16;
        }
    }

    private void ListBox_OnLoaded(object sender, RoutedEventArgs e)
    {
        var scrollViewer = GetScrollViewer(SlideThumbListBox);
        scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
    }

    public static ScrollViewer? GetScrollViewer(DependencyObject? parent)
    {
        if (parent == null)
            return null;
        var count = VisualTreeHelper.GetChildrenCount(parent);

        for (int i = 0; i < count; i++)
        {
            var item = VisualTreeHelper.GetChild(parent, i);
            if (item is ScrollViewer viewer)
            {
                return viewer;
            }
            else
            {
                return GetScrollViewer(item);
            }
        }
        return null;
    }

    private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        var scrollViewer = (ScrollViewer) sender;
        const int limitMaxValue = 20;

        if (_lastElement is not null)
        {
            var point = ShowInTop(_lastElement);
            if (point.Y > _lastElement.ActualHeight / 2 || point.Y < (-1 * _lastElement.ActualHeight * 0.7))
            {
                // 超过一半宽度高度了，那就忽略了吧
            }
            else
            {
                // 保持这一个不动，还没有滚动到看不见
                return;
            }
        }

        foreach (var item in SlideThumbListBox.ItemContainerGenerator.Items.Reverse())
        {
            var listItem = SlideThumbListBox.ItemContainerGenerator.ContainerFromItem(item);

            if (listItem is FrameworkElement element)
            {
                var translatePoint = ShowInTop(element);

                if (translatePoint.Y < limitMaxValue && translatePoint.Y >= 0)
                {
                    if (element != _lastElement)
                    {
                        _lastElement = element;
                        Debug.WriteLine($"滚动到: {item}");
                        //ScrollItemChanged?.Invoke(this, new ScrollItemChangedEventArgs(item, element));
                    }
                    return;
                }
            }
        }

        Point ShowInTop(UIElement element)
        {
            var translatePoint = element.TranslatePoint(new Point(), scrollViewer);
            return translatePoint;
        }
    }

    private FrameworkElement? _lastElement;

}

public class Model
{
    public Model(BitmapSource image, int index)
    {
        Image = image;
        Index = index;
    }

    public int Index { get; }

    public BitmapSource Image { set; get; }

    public override string ToString() => Index.ToString();
}