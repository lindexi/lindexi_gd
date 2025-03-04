using System.Windows;
using System.Windows.Media;

namespace LurlowhairwiwenawhiFeajobaihere;

public class VisualCanvas : FrameworkElement
{
    protected override Visual GetVisualChild(int index)
    {
        return Visual;
    }

    protected override int VisualChildrenCount => 1;

    public VisualCanvas(DrawingVisual visual)
    {
        Visual = visual;
        AddVisualChild(visual);
    }

    public DrawingVisual Visual { get; }
}