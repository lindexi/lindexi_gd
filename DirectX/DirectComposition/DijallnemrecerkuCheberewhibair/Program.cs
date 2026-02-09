// See https://aka.ms/new-console-template for more information

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace DijallnemrecerkuCheberewhibair;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
        {
            return;
        }

        var directCompositionDemo = new DirectCompositionDemo();
        directCompositionDemo.Run();
    }
}