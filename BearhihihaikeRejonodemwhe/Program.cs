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
}

record InputInfo();
record OutputInfo();