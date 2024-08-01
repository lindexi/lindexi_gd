using System;
using System.Windows;
using System.Windows.Media;

namespace LightTextEditorPlus.Document
{
    internal static class GlyphExtension
    {
        /// <summary>
        /// 获取<see cref="GlyphRun"/>的Bounds
        /// </summary>
        /// <param name="run"></param>
        /// <returns></returns>
        public static Rect GetBounds(this GlyphRun run)
        {
            var box = run.ComputeInkBoundingBox();

            //相对于run.BuildGeometry().Bounds方法，run.ComputeInkBoundingBox()会多出一个厚度为1的框框，所以要减去
            if (box.Width >= 2 && box.Height >= 2)
            {
                box.Inflate(-1, -1);
            }

            return box;
        }

        /// <summary>
        /// 获取指定字体的baseline
        /// </summary>
        /// <param name="fontFamily"></param>
        /// <param name="fontRenderingEmSize"></param>
        /// <returns></returns>
        public static double GetBaseline(this FontFamily fontFamily, double fontRenderingEmSize)
        {
            var baseline = fontFamily.Baseline;

            var renderingEmSize = fontRenderingEmSize;

            var value = baseline * renderingEmSize;
            return RefineValue(value);
        }

        /// <summary>
        /// 获取<see cref="GlyphRun"/>的Size
        /// </summary>
        /// <param name="run"></param>
        /// <param name="lineSpacing"></param>
        /// <returns></returns>
        public static Size GetSize(this GlyphRun run, double lineSpacing)
        {
            var renderingEmSize = run.FontRenderingEmSize;
            var height = lineSpacing * renderingEmSize;
            double width = 0;
            foreach (var index in run.GlyphIndices)
            {
                width += run.GlyphTypeface.AdvanceWidths[index];
            }

            width = width * renderingEmSize;
            height = RefineValue(height);
            width = RefineValue(width);
            return new Size(width, height);
        }

        public static double RefineValue(double i)
        {
            var value = IdealToRealWithNoRounding(RealToIdeal(i));

            if (i > 0)
            {
                // Non-zero values should not be converted to 0 accidentally through rounding, ensure that at least the min value is returned.
                value = Math.Max(value, DefaultIdealToReal);
            }

            return value;
        }

        private static int RealToIdeal(double i)
        {
            int value = (int) Math.Round(i * DefaultRealToIdeal);
            if (i > 0)
            {
                // Non-zero values should not be converted to 0 accidentally through rounding, ensure that at least the min value is returned.
                value = Math.Max(value, 1);
            }

            return value;
        }

        /// <summary>
        /// Scale LS ideal resolution value to real value
        /// </summary>
        private static double IdealToRealWithNoRounding(double i)
        {
            return i * DefaultIdealToReal;
        }

        private const double DefaultRealToIdeal = 28800.0 / 96;
        private const double DefaultIdealToReal = 1 / DefaultRealToIdeal;
    }
}