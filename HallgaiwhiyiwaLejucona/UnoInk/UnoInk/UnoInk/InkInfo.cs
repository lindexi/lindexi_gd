namespace UnoInk;

public class InkInfo
{
    public FrameworkElement? InkElement { set; get; }
    public List<StrokePoint> PointList { get; } = new List<StrokePoint>();
}