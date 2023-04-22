using System;
using System.Diagnostics;

namespace WhirawahereRallcobaiwe;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        App app = new App();
        var stopwatch = Stopwatch.StartNew();
        app.InitializeComponent();
        stopwatch.Stop();
        Debug.WriteLine(stopwatch.Elapsed);
        Debug.WriteLine($"{stopwatch.ElapsedMilliseconds}");
        app.Run();
    }
}