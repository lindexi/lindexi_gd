using System.Windows;
using System.Windows.Controls;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Handlers;
using Page = Microsoft.Maui.Controls.Page;
using Rect = Microsoft.Maui.Graphics.Rect;
using Size = Microsoft.Maui.Graphics.Size;

namespace MauiWpfAdapt.Handlers;

class FooButtonHandler : ButtonHandler
{
    public FooButtonHandler() : base(new PropertyMapper<IButton, IButtonHandler>(ButtonHandler.Mapper)
    {
        [nameof(IText.Text)] = MapFooText
    })
    {
    }

    private static void MapFooText(IButtonHandler buttonHandler, IButton button)
    {
        var fooButtonHandler = (FooButtonHandler) buttonHandler;
        if (button is IText text)
        {
            fooButtonHandler.Button.Content = text.Text;
        }
        //button.InvalidateArrange();

        //var mauiButton = (Microsoft.Maui.Controls.Button) button;
        //mauiButton.PlatformSizeChanged();

        //IElement? current = button;
        //while (current != null)
        //{
        //    if (current is Page page)
        //    {
        //        page.Layout(new Rect(0, 0, 100, 100));
        //    }

        //    current = current.Parent;
        //}

        //if (button.Parent is Layout  visualElement)
        //{
        //    visualElement.InvalidateMeasureNonVirtual(InvalidationTrigger.MarginChanged);
        //}
    }

    protected override void ConnectHandler(object platformView)
    {
        var button = (System.Windows.Controls.Button) platformView;
        button.Click += OnClick;
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        VirtualView.Clicked();
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        Button.Measure(new System.Windows.Size(widthConstraint, heightConstraint));
        return new Size(Button.DesiredSize.Width + 100, Button.DesiredSize.Height);
    }

    public override void PlatformArrange(Rect rect)
    {
        base.PlatformArrange(rect);

        Button.SetValue(Canvas.LeftProperty, rect.Left);
        Button.SetValue(Canvas.TopProperty, rect.Top);

        Button.Width = rect.Width;
        Button.Height = rect.Height;
    }

    private System.Windows.Controls.Button Button => (System.Windows.Controls.Button) PlatformView;

    protected override object CreatePlatformView()
    {
        return new System.Windows.Controls.Button();
    }
}