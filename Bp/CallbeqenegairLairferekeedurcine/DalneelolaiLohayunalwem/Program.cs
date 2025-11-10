using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

// 架构：输入2 -> 隐藏层(2) -> 隐藏层(2) -> 输出1
// 需要更多隐藏层时，只需修改下面的数组，比如 new[] { 2, 4, 3, 2, 1 }
var architecture = new[] { 3, 100, 100, 100, 1 };

var net = new Mlp(architecture, seed: 123);

double lr = 0.5;
int maxIterator = 100_000;
double tol = 1e-7;
int iterator = 0;

var testInfoList = new List<TestInfo>();

for (int i = 0; i < 50; i++)
{
    double x0 = Random.Shared.Next(0, 3000) / 10000.0;
    double x1 = Random.Shared.Next(0, 3000) / 10000.0;
    double x2 = Random.Shared.Next(0, 3000) / 10000.0;

    double y_out = x0 + x1 + x2;

    testInfoList.Add(new TestInfo(x0, x1, x2, y_out));
}

double[] x = [0.35, 0.9, 0.5];
double[] target = [0.5];
var stopwatch = Stopwatch.StartNew();
for (; iterator < maxIterator; iterator++)
{
    double c = 0;

    foreach (var testInfo in testInfoList)
    {
        x[0] = testInfo.X0;
        x[1] = testInfo.X1;
        x[2] = testInfo.X2;
        target[0] = testInfo.Y_Out;

        var y = net.Forward(x);
        double cost = 0.5 * Math.Pow(y[0] - target[0], 2);

        var y_out = y[0];
        var t = target[0];

        var s = Math.Abs(t - y_out);
        if (s < 0.00001)
        {
            cost = 0;
        }

        c += cost;

        net.Backward(target, lr);
    }

    var ave = c / testInfoList.Count;

    if (ave < tol)
    {
        break;
    }

    if (iterator == 10000)
    {
        stopwatch.Stop();

        Console.WriteLine($"平均误差 {ave} 单次耗时 {stopwatch.Elapsed.TotalMilliseconds/iterator}ms");
        iterator = 0;

        stopwatch.Restart();
    }
}

Console.WriteLine($"训练结束: 迭代={iterator}");

foreach (var testInfo in testInfoList)
{
    x[0] = testInfo.X0;
    x[1] = testInfo.X1;
    x[2] = testInfo.X2;
    target[0] = testInfo.Y_Out;

    var y = net.Forward(x);
    var s = Math.Abs(target[0] - y[0]);

    Console.WriteLine($"预期={target[0]:F6} 预测={y[0]:F6} 差距={s:F6}");
}

// ----------------- 通用多层 MLP 实现 -----------------
sealed class Mlp
{
    private readonly int[] _sizes; // 每层神经元个数（含输入与输出）
    private List<double[,]> Weight { get; } // 每层权重矩阵: [out, in]
    private readonly List<double[]> _b; // 每层偏置: [out]
    private readonly List<double[]> _a; // 激活缓存（含输入层），a[0]是输入
    private readonly List<double[]> _z; // z 缓存（不含输入层）
    private readonly List<double[]> _delta; // 误差项缓存（不含输入层）

    public Mlp(int[] layerSizes, int seed = 0, double initScale = 0.5)
    {
        if (layerSizes is null || layerSizes.Length < 2)
            throw new ArgumentException("layerSizes 至少包含输入与输出两层。");

        _sizes = (int[]) layerSizes.Clone();
        int layerCount = _sizes.Length - 1; // 权重层数

        Weight = new List<double[,]>(layerCount);
        _b = new List<double[]>(layerCount);
        _a = new List<double[]>(_sizes.Length);
        _z = new List<double[]>(layerCount);
        _delta = new List<double[]>(layerCount);

        var rnd = seed == 0 ? new Random() : new Random(seed);

        // 分配并随机初始化权重与偏置（偏置默认0，权重小随机数）
        for (int l = 0; l < layerCount; l++)
        {
            int inSize = _sizes[l];
            int outSize = _sizes[l + 1];

            var wl = new double[outSize, inSize];
            for (int j = 0; j < outSize; j++)
            {
                for (int i = 0; i < inSize; i++)
                {
                    wl[j, i] = (rnd.NextDouble() - 0.5) * 2 * initScale; // [-initScale, initScale]
                }
            }

            Weight.Add(wl);
            _b.Add(new double[outSize]);
            _z.Add(new double[outSize]);
            _delta.Add(new double[outSize]);
        }

        for (int l = 0; l < _sizes.Length; l++)
        {
            _a.Add(new double[_sizes[l]]);
        }
    }

    // 可选：按示例权重设置（方便与你的三层 BP 对齐）
    public void SetWeights(int layerIndex, double[,] weights, double[] biases)
    {
        if (layerIndex < 0 || layerIndex >= Weight.Count)
            throw new ArgumentOutOfRangeException(nameof(layerIndex));

        var wl = Weight[layerIndex];
        if (wl.GetLength(0) != weights.GetLength(0) || wl.GetLength(1) != weights.GetLength(1))
        {
            throw new ArgumentException("权重矩阵尺寸不匹配。");
        }

        Array.Copy(weights, wl, weights.Length);

        var bl = _b[layerIndex];
        if (bl.Length != biases.Length)
        {
            throw new ArgumentException("偏置长度不匹配。");
        }

        Array.Copy(biases, bl, biases.Length);
    }

    public double[] Forward(ReadOnlySpan<double> input)
    {
        if (input.Length != _sizes[0])
        {
            throw new ArgumentException("输入维度不匹配。");
        }

        // a[0] = input
        input.CopyTo(_a[0]);

        // 逐层前向
        for (int l = 0; l < Weight.Count; l++)
        {
            var wl = Weight[l];
            var bl = _b[l];
            var zl = _z[l];
            var alPrev = _a[l];
            var al = _a[l + 1];

            int outSize = wl.GetLength(0); // 有 outSize 个神经元
            int inSize = wl.GetLength(1); // 每个神经元有 inSize 个输入

            Parallel.For(0, outSize, ParallelOptions, j =>
            {
                double sum = bl[j];
                for (int i = 0; i < inSize; i++)
                {
                    sum += wl[j, i] * alPrev[i];
                }

                zl[j] = sum;
                al[j] = Sigmoid(sum);
            });
        }

        return _a[^1];
    }

    private ParallelOptions ParallelOptions { get; } = new ParallelOptions()
    {
        MaxDegreeOfParallelism = Environment.ProcessorCount
    };

    public void Backward(ReadOnlySpan<double> target, double lr)
    {
        if (target.Length != _sizes[^1])
            throw new ArgumentException("目标维度不匹配。");

        int layerCount = Weight.Count;

        // 输出层 delta
        {
            var aL = _a[^1];
            var zL = _z[^1];
            var dL = _delta[^1];

            for (int j = 0; j < dL.Length; j++)
            {
                double diff = aL[j] - target[j]; // dC/dy
                double dy_dz = aL[j] * (1 - aL[j]); // sigmoid'(z)
                dL[j] = diff * dy_dz;
            }
        }

        // 隐藏层 delta：从倒数第二层权重开始向前
        for (int l = layerCount - 2; l >= 0; l--)
        {
            var wlNext = Weight[l + 1];
            var dlNext = _delta[l + 1];
            var dl = _delta[l];
            var al = _a[l + 1]; // 本层激活（非输入）

            int outSize = dl.Length; // sizes[l+1]
            int nextOut = dlNext.Length; // sizes[l+2]

            Parallel.For(0, outSize, ParallelOptions, i =>
            {
                double sum = 0.0;
                for (int j = 0; j < nextOut; j++)
                {
                    sum += wlNext[j, i] * dlNext[j];
                }

                double dy_dz = al[i] * (1 - al[i]); // sigmoid'(z_l)
                dl[i] = sum * dy_dz;
            });
        }

        // 梯度更新（SGD）
        for (int l = 0; l < layerCount; l++)
        {
            var wl = Weight[l];
            var bl = _b[l];
            var dl = _delta[l];
            var alPrev = _a[l];

            int outSize = wl.GetLength(0);
            int inSize = wl.GetLength(1);

            // dW = delta ⊗ a_prev
            for (int j = 0; j < outSize; j++)
            {
                double dj = dl[j];
                for (int i = 0; i < inSize; i++)
                {
                    wl[j, i] -= lr * dj * alPrev[i];
                }

                bl[j] -= lr * dj;
            }
        }
    }

    private static double Sigmoid(double x) => 1.0 / (1.0 + Math.Exp(-x));
}

record TestInfo(double X0, double X1, double X2, double Y_Out)
{
}