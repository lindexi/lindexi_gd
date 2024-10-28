using System;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;

namespace JallkeleejurCihayaiqalker.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private Vector3DKeyFrameAnimation? _vector3DKeyFrameAnimation;
    private CompositionVisual? _scanBorderCompositionVisual;

    private void ControlButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _scanBorderCompositionVisual = ElementComposition.GetElementVisual(ScanBorder)!;
        var compositor = _scanBorderCompositionVisual.Compositor;
        _vector3DKeyFrameAnimation = compositor.CreateVector3DKeyFrameAnimation();
        _vector3DKeyFrameAnimation.InsertKeyFrame(0f, _scanBorderCompositionVisual.Offset with { Y = 0 });
        _vector3DKeyFrameAnimation.InsertKeyFrame(1f, _scanBorderCompositionVisual.Offset with { Y = this.Bounds.Height - ScanBorder.Height });
        _vector3DKeyFrameAnimation.Duration = TimeSpan.FromSeconds(2);
        _vector3DKeyFrameAnimation.IterationBehavior = AnimationIterationBehavior.Count;
        _vector3DKeyFrameAnimation.IterationCount = 30;

        _scanBorderCompositionVisual.StartAnimation("Offset", _vector3DKeyFrameAnimation);
    }
}
