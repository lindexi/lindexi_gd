using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace NarjejerechowainoBuwurjofear.Inking.Erasing;

class EraserView : Control
{
    public EraserView()
    {
        Path1 = Geometry.Parse("M0,5.0093855C0,2.24277828,2.2303666,0,5.00443555,0L24.9955644,0C27.7594379,0,30,2.23861485,30,4.99982044L30,17.9121669C30,20.6734914,30,25.1514578,30,27.9102984L30,40.0016889C30,42.7621799,27.7696334,45,24.9955644,45L5.00443555,45C2.24056212,45,0,42.768443,0,39.9906145L0,5.0093855z");
        //skPaint.Color = new SKColor(0, 0, 0, 0x33);
        Path1FillBrush = new SolidColorBrush(new Color(0x33, 0, 0, 0));

        Path2 = Geometry.Parse("M20,29.1666667L20,16.1666667C20,15.3382395 19.3284271,14.6666667 18.5,14.6666667 17.6715729,14.6666667 17,15.3382395 17,16.1666667L17,29.1666667C17,29.9950938 17.6715729,30.6666667 18.5,30.6666667 19.3284271,30.6666667 20,29.9950938 20,29.1666667z M13,29.1666667L13,16.1666667C13,15.3382395 12.3284271,14.6666667 11.5,14.6666667 10.6715729,14.6666667 10,15.3382395 10,16.1666667L10,29.1666667C10,29.9950938 10.6715729,30.6666667 11.5,30.6666667 12.3284271,30.6666667 13,29.9950938 13,29.1666667z");
        Path2FillBrush = new SolidColorBrush(new Color(0x26, 0, 0, 0));

        Path3FillBrush = new SolidColorBrush(new Color(0xFF, 0xF2, 0xEE, 0xEB));

        var bounds = Path1.Bounds.Union(Path2.Bounds);
        Width = bounds.Width;
        Height = bounds.Height;

        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Top;
        IsHitTestVisible = false;

        var translateTransform = new TranslateTransform();
        _translateTransform = translateTransform;
        var scaleTransform = new ScaleTransform();
        _scaleTransform = scaleTransform;
        var transformGroup = new TransformGroup();
        transformGroup.Children.Add(_scaleTransform);
        transformGroup.Children.Add(_translateTransform);
        RenderTransform = transformGroup;

        _currentEraserSize = new Size(Width, Height);
    }

    private readonly TranslateTransform _translateTransform;

    private readonly ScaleTransform _scaleTransform;

    private Geometry Path1 { get; }
    private IBrush Path1FillBrush { get; }

    private Geometry Path2 { get; }
    private IBrush Path2FillBrush { get; }

    private IBrush Path3FillBrush { get; }

    private Size _currentEraserSize;

    public void Move(Point position)
    {
        _translateTransform.X = position.X - _currentEraserSize.Width / 2;
        _translateTransform.Y = position.Y - _currentEraserSize.Height / 2;
    }

    public void SetEraserSize(Size size)
    {
        _scaleTransform.ScaleX = size.Width / Width;
        _scaleTransform.ScaleY = size.Height / Height;

        _currentEraserSize = size;
    }

    public override void Render(DrawingContext context)
    {
        context.DrawGeometry(Path1FillBrush, null, Path1);
        //skPaint.Color = new SKColor(0xF2, 0xEE, 0xEB, 0xFF);
        //skCanvas.DrawRoundRect(1, 1, 28, 43, 4, 4, skPaint);
        context.DrawRectangle(Path3FillBrush, null, new RoundedRect(new Rect(1, 1, 28, 43), 4));
        context.DrawGeometry(Path2FillBrush, null, Path2);
    }
}