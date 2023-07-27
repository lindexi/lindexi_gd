using System;
using System.Collections.Generic;
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
using System.Windows.Threading;

namespace NearcijawlainuhurQerefaiwaju;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    List<TextBox> _textBoxes = new List<TextBox>();
    public MainWindow()
    {
        InitializeComponent();

        var grid = Grid;

        for (int y = 0; y < 10; y++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            for (int x = 0; x < 10; x++)
            {
                if (y == 0)
                {
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                }
                var tb = new TextBox();
                Grid.SetColumn(tb, x);
                Grid.SetRow(tb, y);
                _textBoxes.Add(tb);

                grid.Children.Add(tb);
            }
        }

        // DispatcherPriority.Loaded = 6
        // DispatcherPriority.Render = 7
        // DispatcherPriority.Loaded < DispatcherPriority.Render
        var timer = new DispatcherTimer(DispatcherPriority.Loaded);
        timer.Interval = TimeSpan.FromSeconds(0.01);
        timer.Tick += Timer_Tick;
        timer.Start();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _count++;
        foreach (var tb in _textBoxes)
        {
            var noGCRegionValue = false; //noGCRegion.IsChecked == true;
            var ret = noGCRegionValue && GC.TryStartNoGCRegion(10000000);
            tb.Text = _count.ToString();
            if (ret)
                GC.EndNoGCRegion();
        }

    }

    private long _count;
}
