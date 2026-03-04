using System;
using System.Diagnostics;

namespace NalaylikikiLijinewi.Diagnostics;

class Fps
{
    public void Record()
    {
        _count++;

        if (!_stopwatch.IsRunning)
        {
            _stopwatch.Start();
        }

        if (_stopwatch.Elapsed >= TimeSpan.FromSeconds(1))
        {
            Console.WriteLine($"FPS: {_count/ _stopwatch.Elapsed.TotalSeconds :0.000}");

            _count = 0;
            _stopwatch.Restart();
        }
    }

    private int _count;

    private readonly Stopwatch _stopwatch = new Stopwatch();
}