using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Windows.Win32;

namespace BaculelrelNubileregur;

internal class Program
{
    [HandleProcessCorruptedStateExceptions]
    static void Main(string[] args)
    {
        FilterDelegate lpTopLevelExceptionFilter = MyUnhandledExceptionFilter;
        SetUnhandledExceptionFilter(lpTopLevelExceptionFilter);
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        //SetUnhandledExceptionFilterInner();

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

        GC.KeepAlive(lpTopLevelExceptionFilter);
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
    }

    [DllImport("BeyajaydahifallChecheecaifelwarlerenel.dll")]
    static extern Int16 HeederajiYeafalludall();

    [DllImport("BeyajaydahifallChecheecaifelwarlerenel.dll")]
    static extern void SetUnhandledExceptionFilterInner();

    [DllImport("kernel32.dll")]
    static extern IntPtr SetUnhandledExceptionFilter(FilterDelegate lpTopLevelExceptionFilter);

    static IntPtr MyUnhandledExceptionFilter(IntPtr lpExceptionInfo)
    {
        // handle the exception here
        return IntPtr.Zero;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate IntPtr FilterDelegate(IntPtr exceptionPointers);
}
