using System;

namespace Dx
{
    class Program
    {
        static void Main(string[] args)
        {
            using var directXWindow = new DirectXWindow();
            directXWindow.Run();
        }
    }
}
