using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using FantasticCanvasPlacementExperiment.Lib;

namespace FantasticCanvasPlacementExperiment.WPF;

public partial class MainWindow : Window
{
    private sealed record Item(RectD Bounds, string Label);

    private readonly List<Item> _items = [];
    private double _scale = 1;
    private Vector _translation;
    private Point _startScreen;
    private PointD _startWorld;
    private Vector _startTranslation;
    private RectD _startBounds;
    private int _dragIndex = -1;
    private bool _panning;

    public MainWindow()
    {
        InitializeComponent();
    }

    private string Text(string key) => (string)FindResource(key);

    private PointD ToWorld(Point screen) => new((screen.X - _translation.X) / _scale, (screen.Y - _translation.Y) / _scale);

    private RectD VisibleRange()
    {
        PointD topLeft = ToWorld(new Point());
        return new(topLeft.X, topLeft.Y, Viewport.ActualWidth / _scale, Viewport.ActualHeight / _scale);
    }

    private void OnAdd(object sender, RoutedEventArgs e)
    {
        if (Viewport.ActualWidth <= 0 || Viewport.ActualHeight <= 0) return;
        string key = (string)((Button)sender).Tag;
        SizeD size = key switch
        {
            "Portrait" => new(210, 297),
            "Landscape" => new(480, 270),
            _ => new(1440, 270)
        };
        try
        {
            PointD point = Placement.Find(VisibleRange(), size, _items.Select(item => item.Bounds).ToArray());
            _items.Add(new(new(point.X, point.Y, size.Width, size.Height), $"{Text(key)}  ·  {_items.Count + 1}"));
            Redraw();
        }
        catch (ArgumentException exception)
        {
            ShowFailure(exception);
        }
        catch (OverflowException exception)
        {
            ShowFailure(exception);
        }
    }

    private void ShowFailure(SystemException exception) =>
        MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, Text("Failure"), exception.Message), Text("Title"), MessageBoxButton.OK, MessageBoxImage.Error);

    private void OnViewportSizeChanged(object sender, SizeChangedEventArgs e) => Redraw();

    private void Redraw()
    {
        if (World is null || Viewport.ActualWidth <= 0 || Viewport.ActualHeight <= 0) return;
        World.RenderTransform = new MatrixTransform(_scale, 0, 0, _scale, _translation.X, _translation.Y);
        World.Children.Clear();
        DrawRange(VisibleRange(), Brushes.SteelBlue);
        foreach (Item item in _items)
        {
            Border card = new()
            {
                Width = item.Bounds.Width,
                Height = item.Bounds.Height,
                Background = new SolidColorBrush(Color.FromRgb(231, 239, 252)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 139, 195)),
                BorderThickness = new Thickness(1 / _scale),
                CornerRadius = new CornerRadius(4),
                Child = new TextBlock
                {
                    Text = item.Label,
                    FontSize = 16,
                    Margin = new Thickness(16),
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.FromRgb(36, 70, 115))
                }
            };
            Canvas.SetLeft(card, item.Bounds.X);
            Canvas.SetTop(card, item.Bounds.Y);
            World.Children.Add(card);
        }
        Status.Text = string.Format(CultureInfo.CurrentCulture, Text("Status"), _items.Count, _scale);
    }

    private void DrawRange(RectD range, Brush brush)
    {
        Rectangle outline = new()
        {
            Width = range.Width,
            Height = range.Height,
            Stroke = brush,
            StrokeThickness = 2 / _scale,
            StrokeDashArray = new DoubleCollection { 5, 4 }
        };
        Canvas.SetLeft(outline, range.X);
        Canvas.SetTop(outline, range.Y);
        World.Children.Add(outline);
    }

    private void OnWheel(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
        Point mouse = e.GetPosition(Viewport);
        PointD anchor = ToWorld(mouse);
        _scale = Math.Clamp(_scale * Math.Pow(1.2, e.Delta / 120.0), 0.05, 8);
        _translation = new(mouse.X - anchor.X * _scale, mouse.Y - anchor.Y * _scale);
        Redraw();
        e.Handled = true;
    }

    private void OnPointerDown(object sender, MouseButtonEventArgs e)
    {
        if (_panning || _dragIndex >= 0) return;
        _startScreen = e.GetPosition(Viewport);
        _startWorld = ToWorld(_startScreen);
        if (e.ChangedButton == MouseButton.Right)
        {
            _panning = true;
            _startTranslation = _translation;
        }
        else if (e.ChangedButton == MouseButton.Left)
        {
            _dragIndex = _items.FindLastIndex(item => _startWorld.X >= item.Bounds.X && _startWorld.X <= item.Bounds.Right &&
                                                    _startWorld.Y >= item.Bounds.Y && _startWorld.Y <= item.Bounds.Bottom);
            if (_dragIndex < 0) return;
            _startBounds = _items[_dragIndex].Bounds;
        }
        else return;
        Viewport.CaptureMouse();
        e.Handled = true;
    }

    private void OnPointerMove(object sender, MouseEventArgs e)
    {
        Point screen = e.GetPosition(Viewport);
        if (_panning)
        {
            _translation = _startTranslation + (screen - _startScreen);
            Redraw();
        }
        else if (_dragIndex >= 0)
        {
            PointD world = ToWorld(screen);
            RectD next = _startBounds with { X = _startBounds.X + world.X - _startWorld.X, Y = _startBounds.Y + world.Y - _startWorld.Y };
            _items[_dragIndex] = _items[_dragIndex] with { Bounds = next };
            Redraw();
        }
    }

    private void OnPointerUp(object sender, MouseButtonEventArgs e)
    {
        if ((_panning && e.ChangedButton == MouseButton.Right) || (_dragIndex >= 0 && e.ChangedButton == MouseButton.Left))
            Viewport.ReleaseMouseCapture();
    }

    private void OnLostCapture(object sender, MouseEventArgs e)
    {
        _panning = false;
        _dragIndex = -1;
    }
}
