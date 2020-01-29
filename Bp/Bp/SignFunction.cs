namespace Bp
{
    /// <summary>
    /// 符号函数
    /// </summary>
    public class SignFunction : IActivationFunction
    {
        public double Function(double x)
        {
            return x >= 0 ? 1 : -1;
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
