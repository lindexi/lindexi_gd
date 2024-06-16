using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BearhihihaikeRejonodemwhe;

internal class Program
{
    static void Main(string[] args)
    {
        var point1 = new Point();
        var point2 = new Point();
        var point3 = new Point();


    }
}


class Point
{
    public virtual void SetInput(InputInfo inputInfo)
    {
    }

    public virtual async Task<OutputInfo> RunAsync()
    {
        return default;
    }

    public List<Point> NextPointList { get; } = new List<Point>();
}

record InputInfo();
record OutputInfo();