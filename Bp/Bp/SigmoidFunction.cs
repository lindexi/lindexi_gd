using System;

namespace Bp
{
    public class SigmoidFunction : IActivationFunction
    {
        public double DerivativeX(double x)
        {
            //https://www.cnblogs.com/chenlin163/p/7676939.html
            return Function(x) * (1 - Function(x));
        }

        public double DerivativeY(double y)
        {
            return 0;
        }

        public double Function(double x)
        {
            // https://baike.baidu.com/item/Sigmoid函数/7981407?fr=aladdin
            return 1.0 / (1 + Math.Pow(Math.E, -x));
        }
    }








































































































}
