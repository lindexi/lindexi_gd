using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace BerjearnearheliCallrachurjallhelur
{
    class Program
    {
        [DllImport("BeyajaydahifallChecheecaifelwarlerenel.dll")]
        static extern Int16 HeederajiYeafalludall();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint SetUnhandledExceptionFilter(uint n);

        static int Win32Handler(IntPtr nope)
        {

            return 1; // thats EXCEPTION_EXECUTE_HANDLER, although this wont be called due to the previous line
        }

        [DllImport("kernel32.dll")]
        static extern int UnhandledExceptionFilter([In] ref EXCEPTION_POINTERS
   ExceptionInfo);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate int FilterDelegate(IntPtr exception_pointers);

        struct EXCEPTION_POINTERS
        {
            EXCEPTION_RECORD exceptionRecord;
            CONTEXT contextRecord;

        }

        struct EXCEPTION_RECORD
        {
            UInt32 ExceptionCode;
            UInt32 ExceptionFlags;
            IntPtr pExceptionRecord;
            IntPtr ExceptionAddress;
            UInt32 NumberParameters;
            UIntPtr ExceptionInformation;
        }

        struct CONTEXT
        {
            UInt32 ContextFlags;
            UInt32 Dr0;
            UInt32 Dr1;
            UInt32 Dr2;
            UInt32 Dr3;
            UInt32 Dr6;
            UInt32 Dr7;
            FLOATING_SAVE_AREA FloatSave;
            UInt32 SegGs;
            UInt32 SegFs;
            UInt32 SegEs;
            UInt32 SegDs;
            UInt32 Edi;
            UInt32 Esi;
            UInt32 Ebx;
            UInt32 Edx;
            UInt32 Ecx;
            UInt32 Eax;
            UInt32 Ebp;
            UInt32 Eip;
            UInt32 SegCs;
            UInt32 EFlags;
            UInt32 Esp;
            UInt32 SegSs;
        };

        struct FLOATING_SAVE_AREA
        {
            UInt32 ControlWord;
            UInt32 StatusWord;
            UInt32 TagWord;
            UInt32 ErrorOffset;
            UInt32 ErrorSelector;
            UInt32 DataOffset;
            UInt32 DataSelector;
            byte RegisterArea;
            UInt32 Cr0NpxState;
        };

        private static FilterDelegate _win32Handler;

        [DllImport("kernel32.dll")]
        static extern uint GetLastError();

        [DllImport("kernel32.dll")]
        static extern uint SetErrorMode(uint n);

        static void Main(string[] args)
        {
            try
            {
                FilterDelegate win32Handler = new FilterDelegate(Win32Handler);
                _win32Handler = win32Handler;

                var n = SetUnhandledExceptionFilter(1);
                Console.WriteLine(GetLastError());

                SetErrorMode(1);

                Console.WriteLine(HeederajiYeafalludall());
            }
            catch (Exception)
            {

            }

            Console.Read();
        }
    }
}