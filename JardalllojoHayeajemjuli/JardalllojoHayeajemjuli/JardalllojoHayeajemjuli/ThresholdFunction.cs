namespace JardalllojoHayeajemjuli
{
    /// <summary>
    /// 阈值函数
    /// </summary>
    public class ThresholdFunction
    {
        public double Function(double x)
        {
            return (x >= 0) ? 1 : 0;
        }
    }
}