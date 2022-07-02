using SkiaSharp;

namespace KebeninegeeWaljelluhi;

class SKPaintProvider
{
    public SKPaintProvider(SKPaint? paint = null)
    {
        SKPaintList.Add(paint ?? new SKPaint());
    }

    public List<SKPaint> SKPaintList { get; } = new List<SKPaint>();
    public SKPaintProvider Do<T>(Action<SKPaint, T> handler, params T[] valueList)
    {
        var skPaintList = SKPaintList.ToList();
        SKPaintList.Clear();

        foreach (var value in valueList)
        {
            foreach (var skPaint in skPaintList)
            {
                var newSKPaint = skPaint.Clone();
                handler(newSKPaint, value);
                SKPaintList.Add(newSKPaint);
            }
        }

        return this;
    }
}