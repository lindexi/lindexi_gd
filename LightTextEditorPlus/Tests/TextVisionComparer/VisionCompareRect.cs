namespace TextVisionComparer;

/// <summary>
/// 图片比较的范围
/// </summary>
/// <param name="Left"></param>
/// <param name="Top"></param>
/// <param name="Right"></param>
/// <param name="Bottom"></param>
public readonly record struct VisionCompareRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left + 1; // 为什么需要加一？如果有第 0 和第 1 个像素不同，则一共有两个像素不同
    public int Height => Bottom - Top + 1;

    public int X => Left;

    public int Y => Top;

    public override string ToString()
    {
        return $"X={X};Y={Y};W={Width};H={Height}";
    }
}