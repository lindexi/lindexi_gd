// See https://aka.ms/new-console-template for more information

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

const double x0 = 0.35;
const double x1 = 0.9;

const double y_out = 0.5;

var a = x0;
var b = x1;

var w11 = 0.1;
var w12 = 0.8;

var w21 = 0.4;
var w22 = 0.6;

var weightCount = 2;
var nodeCount = 2;

var input = Matrix.Build.SparseOfRowArrays
 (
[
    [x0],
    [x1],
]);

// w1 layer
var layer1Weight = Matrix.Build.SparseOfRows(nodeCount, weightCount,
[
    [w11, w12],
    [w21, w22],
]);

var w31 = 0.3;
var w32 = 0.9;

// w2 layer
var layer2NodeCount = 1;
var layer2Weight = Matrix.Build.SparseOfRows(layer2NodeCount, weightCount,
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
    var dc_dw2Matrix = dc_dz2 * y1Matrix.Transpose(); // dc/dy2 * dy2/dz2 * dy2/d|w2| = dc/d|w2|
    Matrix<double> layer2Delta = dc_dw2Matrix; // 1x2 矩阵

    // 2x1
    var y1MatrixDerivative = y1Matrix.Map(x => x * (1 - x)); // y1 的导数矩阵

    //var dc_dw11 = (y2 - y_out) * (y2 * (1 - y2)) * w31 * (y0 * (1 - y0)) * a;
    //var dc_dw12 = (y2 - y_out) * (y2 * (1 - y2)) * w31 * (y0 * (1 - y0)) * b;

    //var dc_dw21 = (y2 - y_out) * (y2 * (1 - y2)) * w32 * (y1 * (1 - y1)) * a;
    //var dc_dw22 = (y2 - y_out) * (y2 * (1 - y2)) * w32 * (y1 * (1 - y1)) * b;

    // 由于 layer2 是 1x2 的矩阵，所以 layer1Error 也是 1x2 的矩阵
    // dc/dy2 * dy2/dz2 * dz2/d|y1| = dc/|y1|
    Matrix<double> dc_dy1Matrix = dc_dz2 * layer2Weight; // 反向传播
    // dc/dy2 * dy2/dz2 * dz2/d|y1|* d|y1|/d|z1| = dc/d|z1|
    var dc_dz1Matrix = dc_dy1Matrix.Transpose().PointwiseMultiply(y1MatrixDerivative); // 点乘
    // dc_dz1Matrix 就是
    // | dc_dz2 * layer2[0, 0] * y1MatrixD[0, 0] |
    // | dc_dz2 * layer2[0, 1] * y1MatrixD[1, 0] |
    // =
    // | (y2 - y_out) * (y2 * (1 - y2)) * w31 * (y0 * (1 - y0)) |
    // | (y2 - y_out) * (y2 * (1 - y2)) * w32 * (y1 * (1 - y1)) |

    var dc_dw11 = dc_dz2 * layer2Weight[0, 0] * y1MatrixDerivative[0, 0] * input[0, 0];
    var dc_dw12 = dc_dz2 * layer2Weight[0, 0] * y1MatrixDerivative[0, 0] * input[1, 0];

    var dc_dw21 = dc_dz2 * layer2Weight[0, 1] * y1MatrixDerivative[1, 0] * input[0, 0];
    var dc_dw22 = dc_dz2 * layer2Weight[0, 1] * y1MatrixDerivative[1, 0] * input[1, 0];

    var dLayer1Matrix = Matrix.Build.SparseOfRows(nodeCount, weightCount,
    [
        [dc_dw11, dc_dw12],
        [dc_dw21, dc_dw22],
    ]);

    // dc/d|z1| * d|z1|/d|w1| = dc/d|w1|
    // input 是 2x1 的，这里需要构成 1x2 的，才能和 dc_dz0z1Matrix 这个 2x1 的矩阵相乘，得到 2x2 的矩阵
    var dc_dw1Matrix = dc_dz1Matrix * input.Transpose();
    if (dc_dw1Matrix.Equals(dLayer1Matrix))
    {
        // 证明和上面手动计算的结果是一样的
    }
    var layer1Delta = dc_dw1Matrix; // 2x2 矩阵

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

