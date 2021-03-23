using System;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace KajijuniLiguqujokemka
{
    class Program
    {
        [HandleProcessCorruptedStateExceptions]
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine(HeederajiYeafalludall());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [DllImport("BeyajaydahifallChecheecaifelwarlerenel.dll")]
        static extern Int16 HeederajiYeafalludall();
    }
}
