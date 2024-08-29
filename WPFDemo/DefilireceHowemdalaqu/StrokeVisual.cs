using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace DefilireceHowemdalaqu;

/// <summary>
///     用于显示笔迹的类
/// </summary>
public class StrokeVisual : DrawingVisual
{
    /// <summary>
    ///     创建显示笔迹的类
    /// </summary>
    public StrokeVisual() : this(new DrawingAttributes()
    {
        Color = Colors.Red,
        FitToCurve = true,
        Width = 5
    })
    {
    }

    /// <summary>
    ///     创建显示笔迹的类
    /// </summary>
    /// <param name="drawingAttributes"></param>
    public StrokeVisual(DrawingAttributes drawingAttributes)
    {
        _drawingAttributes = drawingAttributes;
    }

    private readonly DrawingAttributes _drawingAttributes;

    /// <summary>
    ///     设置或获取显示的笔迹
    /// </summary>
    public Stroke Stroke { set; get; }

    /// <summary>
    ///     在笔迹中添加点
    /// </summary>
    /// <param name="point"></param>
    public void Add(StylusPoint point)
    {
        if (Stroke == null)
        {
            var collection = new StylusPointCollection { point };
            Stroke = new Stroke(collection) { DrawingAttributes = _drawingAttributes };
        }
        else
        {
            Stroke.StylusPoints.Add(point);

            if (Stroke.StylusPoints.Count > 10)
            {
                var newPointList = ApplyMeanFilter(Stroke.StylusPoints.ToList(), 10);
              
                Stroke.StylusPoints = new StylusPointCollection(newPointList);
            }
        }
    }

    public static List<StylusPoint> ApplyMeanFilter(List<StylusPoint> pointList, int step = 10)
    {
        var xList = ApplyMeanFilter(pointList.Select(t => t.X).ToList(), step);
        var yList = ApplyMeanFilter(pointList.Select(t => t.Y).ToList(), step);

        var newPointList = new List<StylusPoint>();
        for (int i = 0; i < xList.Count && i < yList.Count; i++)
        {
            newPointList.Add(new StylusPoint(xList[i], yList[i]));
        }

        return newPointList;
    }

    public static List<double> ApplyMeanFilter(List<double> list, int step)
    {
        var newList = new List<double>(list.Take(step / 2));
        for (int i = step / 2; i < list.Count - step + step / 2; i++)
        {
            newList.Add(list.Skip(i - step / 2).Take(step).Sum() / step);
        }
        newList.AddRange(list.Skip(list.Count - (step - step / 2)));
        return newList;
    }

    /// <summary>
    ///     重新画出笔迹
    /// </summary>
    public void Redraw()
    {
        using var dc = RenderOpen();
        Stroke.Draw(dc);
    }
}