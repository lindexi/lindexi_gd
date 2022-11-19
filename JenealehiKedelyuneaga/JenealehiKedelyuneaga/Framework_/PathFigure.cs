namespace JenealehiKedelyuneaga;

public sealed class PathFigure
{
    public Point StartPoint { get; set; }
    public PathSegmentCollection Segments { get; set; } = new PathSegmentCollection();
    public bool IsClosed { get; set; }
}