using System;

using Uno.UI.Runtime.Skia;

namespace YewhayeheaFichawearde
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.CursorVisible = false;

                var host = new FrameBufferHost(() => new App());
                host.Run();
            }
            finally
            {
                Console.CursorVisible = true;
            }
        }
    }
}
