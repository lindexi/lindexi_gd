using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Windows.Win32;

namespace BaculelrelNubileregur;

internal class Program
{
    [HandleProcessCorruptedStateExceptions]
    static void Main(string[] args)
    {
        SetUnhandledExceptionFilterInner();

        var task = Task.Run(() =>
        {
            try
            {
                Console.WriteLine(HeederajiYeafalludall());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });

        task.Wait();

        Console.Read();
    }

    [DllImport("BeyajaydahifallChecheecaifelwarlerenel.dll")]
    static extern Int16 HeederajiYeafalludall();

    [DllImport("BeyajaydahifallChecheecaifelwarlerenel.dll")]
    static extern void SetUnhandledExceptionFilterInner();
}
