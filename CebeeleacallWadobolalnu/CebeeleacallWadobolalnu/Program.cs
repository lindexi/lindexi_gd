// See https://aka.ms/new-console-template for more information


using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run<BenchmarkCopyAdditionalData>();

public class BenchmarkCopyAdditionalData
{
    [Params(null, new int[] { 1, 2, 10 }, new int[] { 0, 1, 2, 3, 4, 5 },
        new int[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 })]
    // ReSharper disable once InconsistentNaming 作为性能测试注入值
    public int[]? _additionalValues;

    [Benchmark]
    public void CopyAdditionalDataOrigin()
    {
        if (null != _additionalValues)
        {
            int[] newData = new int[_additionalValues.Length];
            for (int x = 0; x < _additionalValues.Length; x++)
            {
                newData[x] = _additionalValues[x];
            }

            _additionalValues = newData;
        }
    }

    [Benchmark]
    public void CopyAdditionalDataNew()
    {
        if (null != _additionalValues)
        {
            int[] newData = new int[_additionalValues.Length];

            _additionalValues.AsSpan().CopyTo(newData.AsSpan());

            _additionalValues = newData;
        }
    }

    [Benchmark]
    public void CopyAdditionalDataArrayCopy()
    {
        if (null != _additionalValues)
        {
            int[] newData = new int[_additionalValues.Length];

            Array.Copy(_additionalValues, newData, _additionalValues.Length);

            _additionalValues = newData;
        }
    }

    [Benchmark]
    public void CopyAdditionalDataClone()
    {
        _additionalValues = (int[]?) _additionalValues?.Clone();
    }
}
