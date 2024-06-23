using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace BawjadurbaWurahuwa;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        CurrentTextBlock.Text = _list[_index].ToString();

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        while (Content is Grid grid)
        {
            var number = _list[_index];

            var maxValue = 1024;

            var cecaqemdarYefarqukeafai =
                grid.Children.OfType<CecaqemdarYefarqukeafai>()
                    .FirstOrDefault(t => t.Collection.Count > 0 && t.Collection[^1] == number);

            cecaqemdarYefarqukeafai ??= grid.Children.OfType<CecaqemdarYefarqukeafai>().Where(t => t.Collection.Count > 0 && t.Collection[^1] > number).MinBy(t => t.Collection[^1]);

            cecaqemdarYefarqukeafai ??= grid.Children.OfType<CecaqemdarYefarqukeafai>()
                .FirstOrDefault(t => t.Collection.Count == 0);

            cecaqemdarYefarqukeafai ??= grid.Children.OfType<CecaqemdarYefarqukeafai>().MinBy(t =>
            {
                if (t.Collection.Count > 0)
                {
                    var lastValue = t.Collection[^1];
                    if (lastValue > number)
                    {
                        return lastValue;
                    }
                    else
                    {
                        return maxValue;
                    }
                }

                return 0;
            });

            if (cecaqemdarYefarqukeafai is null)
            {
                continue;
            }

            cecaqemdarYefarqukeafai.Collection.Add(number);
            Clean(cecaqemdarYefarqukeafai.Collection);

            _index = Random.Shared.Next(_list.Length);
            _count++;
            CurrentTextBlock.Text = $"第 {_count} 次\r\n下一个 {_list[_index]}";

            await Task.Delay(300);


        }
    }

    private void CecaqemdarYefarqukeafai_OnClick(object? sender, CecaqemdarYefarqukeafai e)
    {
        _count++;

        var number = _list[_index];

        e.Collection.Add(number);

        Clean(e.Collection);

        _index++;
        if (_index == _list.Length)
        {
            _index = 0;
        }
        CurrentTextBlock.Text = $"第 {_count} 次\r\n下一个 {_list[_index]}";
    }

    private static void Clean(ObservableCollection<int> collection)
    {
        while (collection.Count > 1)
        {
            var n1 = collection[^1];
            var n2 = collection[^2];

            if (n1 == n2)
            {
                collection.RemoveAt(collection.Count - 1);
                collection.RemoveAt(collection.Count - 1);
                collection.Add(n1 + n2);
            }
            else
            {
                break;
            }
        }

        if (collection[^1] == 1024 * 2)
        {
            collection.Clear();
        }
    }

    private int _index;
    private int _count;

    private readonly int[] _list = new int[] { 1024, 512, 256, 128, 64, 32, 16, 8, 4, 2 };
}
