using System;
using System.Collections.Generic;
using System.Text;

namespace Oxage.Wmf.Primitive
{
    public struct WmfPoint
    {
        public WmfPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get;  }
        public int Y { get;  }
    }
}
