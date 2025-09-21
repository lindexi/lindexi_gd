// See https://aka.ms/new-console-template for more information

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

// 这是输入层的输入值。可以认为是随机的输入值
const double x0 = 0.35;
const double x1 = 0.9;
const double x2 = 0.5;

const double y_out = 0.3;

// 这是一些随意的权重值，实际应用中这些权重值通过随机数先随意创建的
var w11 = 0.1;
var w12 = 0.8;
var w13 = 0.2;

var w21 = 0.4;
var w22 = 0.6;
var w23 = 0.2;

// 输入参数数量
var inputArgumentCount = 3;
// 第一层的权重数量。第一层的权重数量等于输入参数数量
var layer1WeightCount = inputArgumentCount;
// 第一层的节点数量
var layer1NodeCount = 2;

var input = Matrix.Build.SparseOfRowArrays
(
[
    [x0],
    [x1],
    [x2],
]);

// w1 layer
var layer1Weight = Matrix.Build.SparseOfRows(layer1NodeCount, layer1WeightCount,
[
    [w11, w12, w13],
    [w21, w22, w23],
]);

var w31 = 0.3;
var w32 = 0.9;

// w2 layer
// 第二层的权重数量。第二层的权重数量等于第一层的节点数量
var layer2WeightCount = layer1NodeCount;
// 第二层的节点数量
var layer2NodeCount = 1;
var layer2Weight = Matrix.Build.SparseOfRows(layer2NodeCount, layer2WeightCount,
[
    [w31, w32],
]);

var count = 0;

while (true)
{
    //var z0 = w11 * a + w12 * b;
    //var z1 = w21 * a + w22 * b;

    // layer1 2x2
    // input 2x1
    // z1Matrix 2x1
    // 这里的 z1Matrix 表示 z0 和 z1 组成的矩阵
    Matrix<double> z1Matrix = layer1Weight.Multiply(input);

    //var y0 = F(z0);
    //var y1 = F(z1);

    // y1Matrix 2x1
    // 这里的 y1Matrix 表示 y0 和 y1 组成的矩阵，即 z0 和 z1 组成的 z1Matrix 矩阵经过了激活函数 F 之后的结果
    Matrix<double> y1Matrix = GeluMatrix(z1Matrix);

    //var z2 = w31 * y0 + w32 * y1;
    // layer2 1x2
    // z2Matrix 1x1
    // 这里的 z2Matrix 就是 z2 的值，因为是一个 1x1 的矩阵
    var z2Matrix = layer2Weight.Multiply(y1Matrix);
    //if (z2Matrix.RowCount == 1 && z2Matrix.ColumnCount == 1)
    //{
    //    z2 = z2Matrix[0, 0];
    //}

    // y2 就是神经网络的输出
    var z2 = z2Matrix[0, 0];
    var y2 = Gelu(z2);

    var c = C(y2);

    if (c < 0.0000001)
    {
        Console.WriteLine($"训练结束，成功获取接近预期输出。训练次数={count}");
        break;
    }

    double dc_dz2 = (y2 - y_out) * (GeluDerivative(z2)); // dc/dy2 * dy2/dz2 = dc/dy2 * Gelu'(z2)

    //var dc_dw31 = dc_dz2 * y0;
    //var dc_dw32 = dc_dz2 * y1;

    // 为了能够让 dc_dw2Matrix 叠加到 layer2 1x2 矩阵上，需要先将 y1Matrix 转置为 1x2 矩阵，再与 dc_dz2 相乘
    Matrix<double> layer2Delta = dc_dz2 * y1Matrix.Transpose(); // 1x2 矩阵

    // dc/dw11 = dc/dy2 * dy2/dz2 * dz2/dy0 * dy0/dz0 * dz0/dw11
    // (dc/dy2 * dy2/dz2) = dc/dz2 = dc_dz2
    // dc/dw11 = dc_dz2 * dz2/dy0 * dy0/dz0 * dz0/dw11
    // dz2/dy0 = w31 同理 dz2/dy1 = w32 即这一层 dz2/dyx 为 layer2Weight.Transpose() 矩阵
    // dy0/dz0 = Gelu'(z0) 即这一层 dyx/dzx = GeluDerivativeMatrix(z1Matrix) 矩阵
    // dz0/dw11 = a 同理 dz0/dw12 = b 即这一层 dzx/dw1x = input 矩阵
    // 合起来就是 dc/dw11 = dc/dy2 * dy2/dz2 * dz2/dy0 * dy0/dz0 * dz0/dw11
    // = (dc/dy2 * dy2/dz2) * (dz2/dy0) * (dy0/dz0) * (dz0/dw11)
    // = dc_dz2 * layer2Weight.Transpose() \dot GeluDerivativeMatrix(z1Matrix) * input
    // 这里的 layer2Weight.Transpose() \dot GeluDerivativeMatrix(z1Matrix) 只是为了满足矩阵的点乘而已，确保是每个值进行叠加乘法计算，而不是做矩阵乘法。只有在最后一步再对 input 做矩阵乘法，确保乘出一个 2x3 （layer1NodeCount x layer1WeightCount） 的矩阵
    var layer1Delta = dc_dz2 * layer2Weight.Transpose().PointwiseMultiply(GeluDerivativeMatrix(z1Matrix)) *
                      input.Transpose(); 
    // 2x2 矩阵

    //w31 = w31 - dc_dw31;
    //w32 = w32 - dc_dw32;

    layer2Weight = layer2Weight - layer2Delta;

    //w11 = w11 - dc_dw11;
    //w12 = w12 - dc_dw12;

    //w21 = w21 - dc_dw21;
    //w22 = w22 - dc_dw22;

    layer1Weight = layer1Weight - layer1Delta;

    count++;
}

Console.WriteLine("Hello, World!");

double F(double x)
{
    return 1.0 / (1 + Math.Pow(Math.E, -x));
}

Matrix<double> FMatrix(Matrix<double> x)
{
    return x.Map(F);
}

static double C(double y2)
{
    return 1.0 / 2 * Math.Pow((y2 - y_out), 2);
}

Matrix<double> GeluMatrix(Matrix<double> x) => x.Map(Gelu);

// GELU 激活函数实现
static double Gelu(double x)
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

Matrix<double> GeluDerivativeMatrix(Matrix<double> x) => x.Map(GeluDerivative);

// GELU 激活函数的导数实现
static double GeluDerivative(double x)
{
    // GELU 的导数公式：
    // GELU'(x) = Φ(x) + x * φ(x)
    // 其中 Φ(x) 是标准正态分布的累积分布函数，φ(x) 是标准正态分布的概率密度函数。
    // 使用近似公式：
    // GELU'(x) ≈ 0.5 * (1 + tanh(√(2/π) * (x + 0.044715 * x^3))) + 
    //            0.5 * x * (1 - tanh^2(√(2/π) * (x + 0.044715 * x^3))) * √(2/π) * (1 + 3 * 0.044715 * x^2)

    double c = Math.Sqrt(2.0 / Math.PI);
    double x3 = x * x * x;
    double tanhArg = c * (x + 0.044715 * x3);
    double tanhValue = Math.Tanh(tanhArg);

    double firstTerm = 0.5 * (1.0 + tanhValue);
    double secondTerm = 0.5 * x * (1.0 - tanhValue * tanhValue) * c * (1.0 + 3.0 * 0.044715 * x * x);

    return firstTerm + secondTerm;
}
