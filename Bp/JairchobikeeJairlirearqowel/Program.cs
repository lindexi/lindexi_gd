// See https://aka.ms/new-console-template for more information

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

var matrix1 = Matrix.Build.SparseOfRows(1, 2,
[
    [2,2],
]);

var matrix2 = Matrix.Build.SparseOfRows(2, 1,
[
    [2],
    [3]
]);
var matrix3 = matrix1.Multiply(matrix2);

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


var layer1 = Matrix.Build.SparseOfRows(nodeCount, weightCount,
[
    [w11, w12],
    [w21, w22],
]);

var w31 = 0.3;
var w32 = 0.9;

var layer2NodeCount = 1;
var layer2 = Matrix.Build.SparseOfRows(layer2NodeCount, weightCount,
[
    [w31, w32],
]);

var count = 0;

while (true)
{
    var z0 = w11 * a + w12 * b;
    var z1 = w21 * a + w22 * b;

    Matrix<double> z1Matrix = layer1.Multiply(input);

    var y0 = F(z0);
    var y1 = F(z1);

    Matrix<double> y1Matrix = FMatrix(z1Matrix);

    var z2 = w31 * y0 + w32 * y1;
    var z2Matrix = layer2.Multiply(y1Matrix);
    if (z2Matrix.RowCount == 1 && z2Matrix.ColumnCount == 1)
    {
        z2 = z2Matrix[0, 0];
    }

    var y2 = F(z2);

    var c = C(y2);

    if (c < 0.0000001)
    {
        break;
    }

    var dc_dz2 = (y2 - y_out) * (y2 * (1 - y2));

    var dc_dw31 = dc_dz2 * y0;
    var dc_dw32 = dc_dz2 * y1;

    var dc_dw3132Matrix = y1Matrix.Multiply(dc_dz2);

    var dc_dw11 = dc_dz2 * w31 * (y0 * (1 - y0)) * a;
    var dc_dw12 = dc_dz2 * w31 * (y0 * (1 - y0)) * b;

    var dc_dw21 = dc_dz2 * w32 * (y1 * (1 - y1)) * a;
    var dc_dw22 = dc_dz2 * w32 * (y1 * (1 - y1)) * b;

    // 2x1
    var y1MatrixT = y1Matrix.Map(x => x * (1 - x));

    // 已知 e = a x c, f = b x d 。其中 a b 为 2x1 的矩阵 M1，c d 为 1x2 的矩阵 M2。请将 e f 作为矩阵 M3 表示出来，且写出 e f 所在的矩阵 M3 与 M1 M2 的关系。


    w31 = w31 - dc_dw31;
    w32 = w32 - dc_dw32;

    layer2 = layer2 - dc_dw3132Matrix;

    w11 = w11 - dc_dw11;
    w12 = w12 - dc_dw12;

    w21 = w21 - dc_dw21;
    w22 = w22 - dc_dw22;

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

