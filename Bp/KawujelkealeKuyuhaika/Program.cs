// See https://aka.ms/new-console-template for more information

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

const double x0 = 0.35;
const double x1 = 0.9;
const double x2 = 0.5;

const double y_out = 0.5;

var a = x0;
var b = x1;

var w11 = 0.1;
var w12 = 0.8;

var w21 = 0.4;
var w22 = 0.6;


var weightCount = 3;
var nodeCount = 2;

var input = Matrix.Build.SparseOfRowArrays
(
[
    [x0],
    [x1],
    [x2], 
]);

// w1 layer
var layer1Weight = Matrix.Build.SparseOfRows(nodeCount, weightCount,
[
    [w11, w12, 0.2],
    [w21, w22, 0.2],
]);

var w31 = 0.3;
var w32 = 0.9;

// w2 layer
var layer2NodeCount = 1;
var layer2Weight = Matrix.Build.SparseOfRows(layer2NodeCount, 2,
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
    Matrix<double> y1Matrix = FMatrix(z1Matrix);

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
    var y2 = F(z2Matrix[0, 0]);

    var c = C(y2);

    if (c < 0.0000001)
    {
        break;
    }

    double dc_dz2 = (y2 - y_out) * (y2 * (1 - y2)); // dc/dy2 * dy2/dz2

    //var dc_dw31 = dc_dz2 * y0;
    //var dc_dw32 = dc_dz2 * y1;

    // 为了能够让 dc_dw2Matrix 叠加到 layer2 1x2 矩阵上，需要先将 y1Matrix 转置为 1x2 矩阵，再与 dc_dz2 相乘
    Matrix<double> layer2Delta = dc_dz2 * y1Matrix.Transpose(); // 1x2 矩阵

    var layer1Delta = dc_dz2 * layer2Weight.Transpose().PointwiseMultiply(y1Matrix.Map(x => x * (1 - x))) *
                      input.Transpose(); // 2x2 矩阵

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