using DotNetCampus.Numerics;
using DotNetCampus.Numerics.Geometry;

using SkiaSharp;

namespace DotNetCampus.Inking.Utils;

static class SkiaRectExtension
{
    public static SKMatrix ToSkMatrix(this AffineTransformation2D matrix)
    {
        return new SKMatrix
        {
            ScaleX = (float) matrix.M11,
            SkewX = (float) matrix.M12,
            SkewY = (float) matrix.M21,
            ScaleY = (float) matrix.M22,
            TransX = (float) matrix.OffsetX,
            TransY = (float) matrix.OffsetY,
            Persp0 = 0,
            Persp1 = 0,
            Persp2 = 1,
        };
    }

    public static SKMatrix ToSkMatrix(this SimilarityTransformation2D transform)
    {
        return new SKMatrix
        {
            ScaleX = (float) transform.Scaling,
            SkewX = 0,
            SkewY = 0,
            ScaleY = (float) transform.Scaling,
            TransX = (float) transform.Translation.X,
            TransY = (float) transform.Translation.Y,
            Persp0 = 0,
            Persp1 = 0,
            Persp2 = 1,
        };
    }

    public static AffineTransformation2D ToNumericMatrix(this SKMatrix matrix)
    {
        return new AffineTransformation2D(matrix.ScaleX, matrix.SkewX, matrix.SkewY, matrix.ScaleY, matrix.TransX, matrix.TransY);
    }

    public static SimilarityTransformation2D ToSimilarityTransformation(this SKMatrix matrix)
    {
        return matrix.ToNumericMatrix().ToSimilarityTransformation2D();
    }

    /// <summary>
    /// 将 <see cref="AffineTransformation2D" /> 转换为 <see cref="SimilarityTransformation2D" />，其中剪切变换将被忽略，非等比缩放将使用最大缩放比进行等比缩放。
    /// </summary>
    /// <param name="affineTransformation">仿射变换。</param>
    /// <returns>相似变换。</returns>
    public static SimilarityTransformation2D ToSimilarityTransformation2D(this AffineTransformation2D affineTransformation)
    {
        var decomposition = affineTransformation.Decompose();
        var scale = Math.Max(decomposition.Scaling.ScaleX.Abs(), decomposition.Scaling.ScaleY.Abs());
        return new SimilarityTransformation2D(scale, decomposition.Scaling.ScaleY < 0, decomposition.Rotation, decomposition.Translation);
    }
}