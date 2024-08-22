using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BecilanegukemCawhafebe;
class AverageCounter
{
    public AverageCounter(int averageMaxCount, AverageCounterRecordHandler handler)
    {
        _averageMaxCount = averageMaxCount;
        _handler = handler;

        //#if DEBUG
        //        Enable = true;
        //#endif
        Enable = true;
    }

    /// <summary>
    /// 是否可用，用于方便一口气关闭
    /// </summary>
    public bool Enable { get; set; }

    public void Start()
    {
        _stopwatch.Restart();
    }

    public void Stop()
    {
        _stopwatch.Stop();
        _totalTime += _stopwatch.Elapsed.TotalMilliseconds;
        _count++;

        if (_count >= _averageMaxCount)
        {
            _handler(_totalTime / _count);
            _totalTime = 0;
            _count = 0;
        }
    }

    private readonly Stopwatch _stopwatch = new Stopwatch();
    private double _totalTime;
    private int _count;
    private readonly int _averageMaxCount;
    private readonly AverageCounterRecordHandler _handler;
}

delegate void AverageCounterRecordHandler(double averageTime);
