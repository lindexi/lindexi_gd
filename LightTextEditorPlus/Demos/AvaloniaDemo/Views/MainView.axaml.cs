using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;

namespace LightTextEditorPlus.AvaloniaDemo.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        Foo.InvalidateVisual();
    }
}

public class Foo : Control
{
    public override void Render(DrawingContext context)
    {
        base.Render(context);
    }

    class C : ICustomDrawOperation
    {
        public void Dispose()
        {
            
        }

        public bool Equals(ICustomDrawOperation? other)
        {
            return false;
        }

        public bool HitTest(Point p)
        {
            return false;
        }

        public void Render(ImmediateDrawingContext context)
        {
            
        }

        public Rect Bounds { get; }
    }
}