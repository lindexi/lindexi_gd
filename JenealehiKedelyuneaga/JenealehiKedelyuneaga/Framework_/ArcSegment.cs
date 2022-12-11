namespace JenealehiKedelyuneaga;

public class ArcSegment : PathSegment
{
    public Size Size { get; set; }
    public double RotationAngle { get; set; }
    public bool IsLargeArc { get; set; }
    public SweepDirection SweepDirection { get; set; }
    public Point Point { get; set; }
}