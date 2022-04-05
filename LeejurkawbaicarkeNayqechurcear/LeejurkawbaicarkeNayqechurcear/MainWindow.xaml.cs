using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

namespace LeejurkawbaicarkeNayqechurcear;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Button1_Click(object sender, RoutedEventArgs e)
    {
        var list = await Task.Run(() =>
        {
            ObservableCollection<string> data = new ObservableCollection<string>();
            for (int i = 0; i < 100; i++)
            {
                data.Add(Random.Shared.Next(1000).ToString());
            }
            return data;
        });

        // 以上代码使用 await 等待，可以自动切回主线程

        ListView.ItemsSource = list;
    }

    private async void Button2_Click(object sender, RoutedEventArgs e)
    {
        // 假定 ListView.ItemsSource 存在源了
        if (ListView.ItemsSource is not ObservableCollection<string> list)
        {
            // 如果假设失败，强行给一个源
            list = new();
            ListView.ItemsSource = list;
        }

        var newList = await Task.Run(() =>
        {
            var data = new ObservableCollection<string>(list);

            // 模拟对原有的列表进行处理
            if (data.Count > 0)
            {
                for (int i = 0; i < 100; i++)
                {
                    data.Move(Random.Shared.Next(data.Count), Random.Shared.Next(data.Count));
                }
            }

            return data;
        });

        ListView.ItemsSource = newList;
    }

    private async void Button3_Click(object sender, RoutedEventArgs e)
    {
        if (ListView.ItemsSource is not FooList<string> list)
        {
            list = new FooList<string>();

            ListView.ItemsSource = list;
        }

        await Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                list.Add(Random.Shared.Next(100).ToString());
            }
        });

        await Task.Delay(TimeSpan.FromSeconds(1));

        await Task.Run(() =>
        {
            for (int i = 0; i < 10; i++)
            {
                list.RemoveAt(i);
            }
        });
       
        await Task.Delay(TimeSpan.FromSeconds(1));

        await Task.Run(() =>
        {
            for (int i = 0; i < 10; i++)
            {
                list[i] = i.ToString();
            }
        });
    }
}

public class FooList<T> : Collection<T>, INotifyCollectionChanged
{
    protected override void InsertItem(int index, T item)
    {
        base.InsertItem(index, item);

        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            CollectionChanged?.Invoke(this,
          new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        });
    }

    protected override void RemoveItem(int index)
    {
        var item = this[index];

        base.RemoveItem(index);

        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            CollectionChanged?.Invoke(this,
          new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        });
    }

    protected override void SetItem(int index, T item)
    {
        var oldItem = this[index];
        base.SetItem(index, item);
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            CollectionChanged?.Invoke(this,
          new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, oldItem, index));
        });
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
}
