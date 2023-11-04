namespace Bp
{
    /// <summary>
    /// 阈值函数
    /// </summary>
    public class ThresholdFunction : IActivationFunction
    {
        public double Function(double x)
        {
            return (x >= 0) ? 1 : 0;
        }

        public double DerivativeX(double x)
        {
            return 0;
        }

        public double DerivativeY(double y)
        {
            return 0;
        }
    }
}
