using System.Collections.Generic;

namespace OpenMcdf
{
    internal class CFItemComparer : IComparer<ReadonlyCFItem>
    {
        public int Compare(ReadonlyCFItem x, ReadonlyCFItem y)
        {
            // X CompareTo Y : X > Y --> 1 ; X < Y  --> -1
            return (x.DirEntry.CompareTo(y.DirEntry));

            //Compare X < Y --> -1
        }
    }
}