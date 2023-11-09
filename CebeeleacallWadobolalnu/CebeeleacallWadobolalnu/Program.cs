/*
```
   
   BenchmarkDotNet v0.13.10, Windows 11 (10.0.22621.2428/22H2/2022Update/SunValley2)
   13th Gen Intel Core i7-13700K, 1 CPU, 24 logical and 16 physical cores
   .NET SDK 8.0.100-preview.7.23376.3
   [Host]     : .NET 7.0.9 (7.0.923.32018), X64 RyuJIT AVX2
   DefaultJob : .NET 7.0.9 (7.0.923.32018), X64 RyuJIT AVX2
   
   
   ```
   | Method                      | _additionalValues | Mean        | Error     | StdDev    | Median      |
   |---------------------------- |------------------ |------------:|----------:|----------:|------------:|
   | CopyAdditionalDataOrigin    | null              |   0.0977 ns | 0.0029 ns | 0.0027 ns |   0.0973 ns |
   | CopyAdditionalDataNew       | null              |   0.1065 ns | 0.0051 ns | 0.0045 ns |   0.1056 ns |
   | CopyAdditionalDataArrayCopy | null              |   0.1112 ns | 0.0044 ns | 0.0041 ns |   0.1118 ns |
   | CopyAdditionalDataClone     | null              |   2.0188 ns | 0.0063 ns | 0.0059 ns |   2.0185 ns |
   | CopyAdditionalDataOrigin    | Int32[16]         |  10.9633 ns | 0.2001 ns | 0.1774 ns |  10.9225 ns |
   | CopyAdditionalDataNew       | Int32[16]         |   6.8333 ns | 0.1497 ns | 0.3057 ns |   6.7148 ns |
   | CopyAdditionalDataArrayCopy | Int32[16]         |   7.4344 ns | 0.1148 ns | 0.1018 ns |   7.4218 ns |
   | CopyAdditionalDataClone     | Int32[16]         | 128.0257 ns | 0.2681 ns | 0.2376 ns | 128.0255 ns |
   | CopyAdditionalDataOrigin    | Int32[3]          |   5.9837 ns | 0.1535 ns | 0.4429 ns |   5.9883 ns |
   | CopyAdditionalDataNew       | Int32[3]          |   5.8295 ns | 0.1227 ns | 0.1413 ns |   5.8011 ns |
   | CopyAdditionalDataArrayCopy | Int32[3]          |   6.7674 ns | 0.1526 ns | 0.2089 ns |   6.7955 ns |
   | CopyAdditionalDataClone     | Int32[3]          |  36.9665 ns | 0.1484 ns | 0.1388 ns |  36.9879 ns |
   | CopyAdditionalDataOrigin    | Int32[6]          |   6.9431 ns | 0.1220 ns | 0.1141 ns |   6.9015 ns |
   | CopyAdditionalDataNew       | Int32[6]          |   5.7180 ns | 0.1062 ns | 0.0994 ns |   5.6906 ns |
   | CopyAdditionalDataArrayCopy | Int32[6]          |   6.6655 ns | 0.1490 ns | 0.1394 ns |   6.6233 ns |
   | CopyAdditionalDataClone     | Int32[6]          |  36.6128 ns | 0.2452 ns | 0.2174 ns |  36.5959 ns |
   
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
