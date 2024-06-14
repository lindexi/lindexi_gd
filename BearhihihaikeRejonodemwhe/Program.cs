using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BearhihihaikeRejonodemwhe;

internal class Program
{
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