using System;

namespace Bp
{
    public class DoubleRange
    {
        public DoubleRange(double a, double b)
        {
            Max = Math.Max(a, b);
            Min = Math.Min(a, b);

            Length = Max - Min;
        }

        public double Length { get; }
        public double Max { get; }
        public double Min { get; }

        /// <summary>
        /// 返回在 Min 和 Max 的随机数
        /// </summary>
        /// <returns></returns>
        public double GetRan()
        {
            return Rand.NextDouble() * Length + Min;
        }

        /// <summary>
        /// 随机数生成器
        /// </summary>
        private static Random Rand { get; set; } = new Random();
    }








































































































}
