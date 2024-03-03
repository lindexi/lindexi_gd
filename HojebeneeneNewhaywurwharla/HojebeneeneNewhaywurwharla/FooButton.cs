using Microsoft.UI.Xaml.Controls.Primitives;

namespace HojebeneeneNewhaywurwharla;

public partial class FooButton : ButtonBase
{
    public FooButton()
    {
        DefaultStyleKey = typeof(FooButton);
    }

    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill), typeof(Brush), typeof(FooButton), new PropertyMetadata(default(Brush)));

    public Brush Fill
    {
        get { return (Brush)GetValue(FillProperty); }
        set { SetValue(FillProperty, value); }
    }
}
