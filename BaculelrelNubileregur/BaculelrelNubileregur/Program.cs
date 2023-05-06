using System.Reflection;
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
                //Console.WriteLine(HeederajiYeafalludall());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });

        task.Wait();

        var thread = new Thread(() =>
        {
            throw new Exception();
        });
        thread.Start();
        thread.Join();

        Console.Read();

        GC.KeepAlive(lpTopLevelExceptionFilter);
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        e.GetType().GetField("_isTerminating", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(e, new object[] { false });
        //e.GetType().GetMethod("SetIsTerminating", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(e, new object[] { false });
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
