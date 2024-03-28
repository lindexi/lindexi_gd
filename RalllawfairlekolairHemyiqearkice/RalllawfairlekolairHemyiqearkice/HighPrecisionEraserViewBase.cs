namespace RalllawfairlekolairHemyiqearkice;

/// <summary>
/// 高精度橡皮擦的界面
/// </summary>
public partial class HighPrecisionEraserViewBase : ContentControl, IEraserView
{
    public HighPrecisionEraserViewBase()
    {
        TranslateTransform = new TranslateTransform();
        RotateTransform = new RotateTransform();
        ScaleTransform = new ScaleTransform();

        TransformGroup = new TransformGroup()
        {
            Children = new TransformCollection()
            {
                ScaleTransform,
                RotateTransform,
                TranslateTransform,
            }
        };

        RenderTransform = TransformGroup;

        // 设置命中测试，用来提升性能
        IsHitTestVisible = false;

        RegisterPropertyChangedCallback(WidthProperty,
            (sender, dp) => ((HighPrecisionEraserViewBase)sender).UpdateTransform());
        RegisterPropertyChangedCallback(HeightProperty,
            (sender, dp) => ((HighPrecisionEraserViewBase) sender).UpdateTransform());
    }

    private static PropertyMetadata AffectsRenderDoublePropertyMetadata =>
        new PropertyMetadata(default(double), OnPropertyChanged);

    public static readonly DependencyProperty XProperty = DependencyProperty.Register(
        "X", typeof(double), typeof(HighPrecisionEraserViewBase), AffectsRenderDoublePropertyMetadata);

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((HighPrecisionEraserViewBase) d).UpdateTransform();
    }

    public double X
    {
        get { return (double) GetValue(XProperty); }
        set { SetValue(XProperty, value); }
    }

    public static readonly DependencyProperty YProperty = DependencyProperty.Register(
        "Y", typeof(double), typeof(HighPrecisionEraserViewBase), AffectsRenderDoublePropertyMetadata);

    public double Y
    {
        get { return (double) GetValue(YProperty); }
        set { SetValue(YProperty, value); }
    }

    public static readonly DependencyProperty RotationProperty = DependencyProperty.Register(
        "Rotation", typeof(double), typeof(HighPrecisionEraserViewBase),
        AffectsRenderDoublePropertyMetadata);

    double IEraserView.Rotation
    {
        get { return (double) GetValue(RotationProperty); }
        set { SetValue(RotationProperty, value); }
    }

    //public new static readonly DependencyProperty WidthProperty = DependencyProperty.Register(
    //    "Width", typeof(double), typeof(HighPrecisionEraserViewBase), AffectsRenderDoublePropertyMetadata);

    //double IEraserView.Width
    //{
    //    get { return (double) GetValue(WidthProperty); }
    //    set { SetValue(WidthProperty, value); }
    //}

    //public new static readonly DependencyProperty HeightProperty = DependencyProperty.Register(
    //    "Height", typeof(double), typeof(HighPrecisionEraserViewBase),
    //    AffectsRenderDoublePropertyMetadata);

    //double IEraserView.Height
    //{
    //    get { return (double) GetValue(HeightProperty); }
    //    set { SetValue(HeightProperty, value); }
    //}

    private TranslateTransform TranslateTransform { get; }
    private RotateTransform RotateTransform { get; }
    private ScaleTransform ScaleTransform { get; }

    private TransformGroup TransformGroup { get; }

    //protected override void OnRender(DrawingContext drawingContext)

    private void UpdateTransform()
    {
        var contentWidth = double.NaN;
        var contentHeight = double.NaN;
        if (Content is FrameworkElement frameworkElement)
        {
            contentWidth = frameworkElement.ActualWidth;
            contentHeight = frameworkElement.ActualHeight;
        }

        IEraserView eraserView = this;

        var width = eraserView.Width;
        var height = eraserView.Height;
        var scaleTransform = ScaleTransform;
        if (double.IsNaN(width) || double.IsNaN(height) || double.IsNaN(contentWidth) || double.IsNaN(contentHeight))
        {
            scaleTransform.ScaleX = 1;
            scaleTransform.ScaleY = 1;
        }
        else
        {
            scaleTransform.ScaleX = width / contentWidth; //bounds.Width;
            scaleTransform.ScaleY = height / contentHeight; //bounds.Height;
        }

        TranslateTransform.X = eraserView.X;
        TranslateTransform.Y = eraserView.Y;

        //Debug.WriteLine($"Rotation={Rotation}");
        RotateTransform.Angle = eraserView.Rotation;
        RotateTransform.CenterX = width / 2;
        RotateTransform.CenterY = height / 2;
    }
}
