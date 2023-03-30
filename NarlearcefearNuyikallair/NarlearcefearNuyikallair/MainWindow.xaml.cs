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

namespace NarlearcefearNuyikallair;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Width = 1000;
        WindowState = WindowState.Maximized;

        MouseDown += MainWindow_MouseDown;
    }

    private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
    {
        UpdateElement();
    }

    private void UpdateElement()
    {
        var grid = (Grid) Content;
        grid.Children.Clear();
        grid.Children.Add(new F());
    }
}


public class F : FrameworkElement
{
    public F()
    {
        IsHitTestVisible = false;
    }

    private int _count;

    protected override void OnRender(DrawingContext drawingContext)
    {
        for (int i = 0; i < 100000; i++)
        {
            Brush brush = _count switch
            {
                0 => Brushes.Black,
                _ => Brushes.Red,
            };

            var canvasWidth = 1000.0 / 3;

            drawingContext.DrawRectangle(brush, null, new Rect(i % canvasWidth * 3, i / canvasWidth * 3, 2, 2));

            _count++;
            if (_count > 1)
            {
                _count = 0;
            }
        }

        // 让下次变更颜色
        _count++;
        if (_count > 1)
        {
            _count = 0;
        }
    }
}