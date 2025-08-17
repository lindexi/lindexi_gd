using System;

// See https://aka.ms/new-console-template for more information
class Program
{
    // GELU 激活函数实现
    public static double Gelu(double x)
    {
        //  \text{GELU}(x) = x \cdot \Phi(x)
        // 其中 \Phi(x) 是标准正态分布的累积分布函数
        // 近似公式：\Phi(x) \approx 0.5 \cdot x \cdot (1 + \tanh(c \cdot (x + 0.044715 \cdot x^3)))
        //  \Phi(x) \approx 0.5 \left[1 + \tanh\left(\sqrt{\frac{2}{\pi}}(x + 0.044715x^3)\right)\right]
        // 近似公式实现
        double c = Math.Sqrt(2.0 / Math.PI);
        double x3 = x * x * x;
        double tanhArg = c * (x + 0.044715 * x3);
        double result = 0.5 * x * (1.0 + Math.Tanh(tanhArg));
        return result;
    }

    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        // 多组输入数据用于测试 GELU 激活函数
        double[] testInputs = { -3.0, -1.0, 0.0, 0.5, 1.0, 2.0, 3.0 };

        foreach (double input in testInputs)
        {
            double output = Gelu(input);
            Console.WriteLine($"GELU({input}) = {output}");
        }
    }
}
