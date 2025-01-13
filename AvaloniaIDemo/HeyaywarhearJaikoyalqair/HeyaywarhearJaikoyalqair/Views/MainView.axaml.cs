using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Media;

namespace HeyaywarhearJaikoyalqair.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        var imeSupporter = new IMESupporter(RootGrid);
        _imeSupporter = imeSupporter;
        RootGrid.TextInputMethodClientRequested += (sender, args) =>
        {
            args.Client = imeSupporter;
        };
        RootGrid.Focusable = true;
    }

    private readonly IMESupporter _imeSupporter;

    private void RootGrid_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var (x, y) = e.GetPosition(RootGrid);
        var translateTransform = (TranslateTransform) DebugBorder.RenderTransform!;
        translateTransform.X = x;
        translateTransform.Y = y;

        RootGrid.Focus(NavigationMethod.Pointer);

        _imeSupporter.SetCursorRectangle(new Rect(x, y, 1, 1));
    }
}

internal class IMESupporter : TextInputMethodClient
{
    public IMESupporter(Visual textViewVisual)
    {
        TextViewVisual = textViewVisual;
    }

    public void SetCursorRectangle(Rect cursorRectangle)
    {
        _cursorRectangle = cursorRectangle;
        RaiseCursorRectangleChanged();
    }

    public override Visual TextViewVisual { get; }
    public override bool SupportsPreedit => false;
    public override bool SupportsSurroundingText => false;
    public override string SurroundingText => string.Empty;
    public override Rect CursorRectangle => _cursorRectangle;
    private Rect _cursorRectangle;
    public override TextSelection Selection { get; set; }
}