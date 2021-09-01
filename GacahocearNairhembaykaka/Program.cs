using System;
using System.IO;
using OpenMcdf;

namespace GacahocearNairhembaykaka
{
    class Program
    {
        static void Main(string[] args)
        {
            CompoundFile cf = new CompoundFile("oleObject1.bin");
            var rootStorage = cf.RootStorage;
            var cfStream = rootStorage.GetStream("Package");
            var data = cfStream.GetData();

        }
    }
}
