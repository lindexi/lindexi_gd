/*
```
   
   BenchmarkDotNet v0.13.10, Windows 11 (10.0.22621.2428/22H2/2022Update/SunValley2)
   13th Gen Intel Core i7-13700K, 1 CPU, 24 logical and 16 physical cores
   .NET SDK 8.0.100-preview.7.23376.3
   [Host]     : .NET 7.0.9 (7.0.923.32018), X64 RyuJIT AVX2
   DefaultJob : .NET 7.0.9 (7.0.923.32018), X64 RyuJIT AVX2
   
   
   ```
   | Method                        | _additionalValues | Mean       | Error     | StdDev    | Median     |
   |------------------------------ |------------------ |-----------:|----------:|----------:|-----------:|
   | CopyAdditionalDataOrigin      | ?                 |  0.0963 ns | 0.0017 ns | 0.0016 ns |  0.0964 ns |
   | CopyAdditionalDataNew         | ?                 |  0.1043 ns | 0.0034 ns | 0.0032 ns |  0.1035 ns |
   | CopyAdditionalDataSpanToArray | ?                 |  1.6303 ns | 0.0032 ns | 0.0025 ns |  1.6300 ns |
   | CopyAdditionalDataArrayCopy   | ?                 |  0.1085 ns | 0.0012 ns | 0.0011 ns |  0.1086 ns |
   | CopyAdditionalDataOrigin      | Int32[16]         | 10.7999 ns | 0.0936 ns | 0.0876 ns | 10.7816 ns |
   | CopyAdditionalDataNew         | Int32[16]         |  6.6402 ns | 0.0698 ns | 0.0653 ns |  6.6059 ns |
   | CopyAdditionalDataSpanToArray | Int32[16]         |  6.9252 ns | 0.0778 ns | 0.0728 ns |  6.9060 ns |
   | CopyAdditionalDataArrayCopy   | Int32[16]         |  7.6017 ns | 0.0576 ns | 0.0640 ns |  7.5834 ns |
   | CopyAdditionalDataOrigin      | Int32[3]          |  5.8107 ns | 0.1385 ns | 0.3599 ns |  5.7610 ns |
   | CopyAdditionalDataNew         | Int32[3]          |  5.7859 ns | 0.1201 ns | 0.2135 ns |  5.7399 ns |
   | CopyAdditionalDataSpanToArray | Int32[3]          |  6.3063 ns | 0.0816 ns | 0.0763 ns |  6.2863 ns |
   | CopyAdditionalDataArrayCopy   | Int32[3]          |  6.8956 ns | 0.1596 ns | 0.2389 ns |  6.8104 ns |
   | CopyAdditionalDataOrigin      | Int32[6]          |  7.3018 ns | 0.1668 ns | 0.2833 ns |  7.2124 ns |
   | CopyAdditionalDataNew         | Int32[6]          |  6.6379 ns | 0.2531 ns | 0.7344 ns |  6.4139 ns |
   | CopyAdditionalDataSpanToArray | Int32[6]          |  6.2877 ns | 0.1469 ns | 0.1961 ns |  6.2389 ns |
   | CopyAdditionalDataArrayCopy   | Int32[6]          |  6.9187 ns | 0.1461 ns | 0.1740 ns |  6.8639 ns |
   
 */


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
    public void CopyAdditionalDataSpanToArray()
    {
        _additionalValues = _additionalValues?.AsSpan().ToArray();
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
}
