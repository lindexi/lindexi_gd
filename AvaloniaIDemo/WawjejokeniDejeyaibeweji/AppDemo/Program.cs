using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppDemo;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AvaloniaIDemo.Initializer.InitAssembly();

        UnoDemo.Program.Main(args);
    }
}
