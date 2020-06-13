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
            Console.WriteLine(DateTime.Now.ToString("MMM"));
            return;

            Console.WriteLine(OptimizationSize(new Size(2, 100), new Size(100, 100), new Size(500, 500)));

            Console.WriteLine(OptimizationSize(new Size(2, 100), new Size(100, 200), new Size(500, 500)));
            Console.WriteLine(OptimizationSize(new Size(2, 10), new Size(100, 200), new Size(1000, 1000)));
            

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

        /// <summary>
        /// 宽度和高度不小于最小大小，但是不大于最大大小，缩放使用等比缩放
        /// <para/>
        /// 规则：
        /// <para/>
        /// - 如果有一边小于最小大小，那么缩放到这一边大于等于最小大小
        /// <para/>
        /// - 如果一边缩放之后大于最大的大小，那么限制不能超过最大的大小
        /// <para/>
        /// - 尽可能让大小接近最小大小，但是保证宽度和高度都不大于最大大小
        /// </summary>
        /// <param name="currentSize"></param>
        /// <param name="minSize"></param>
        /// <param name="maxSize"></param>
        /// <returns></returns>
        static Size OptimizationSize(Size currentSize, Size minSize, Size maxSize)
        {
            if (currentSize.Width > minSize.Width && currentSize.Height > minSize.Height)
            {
                if (currentSize.Width <= maxSize.Width && currentSize.Height <= maxSize.Height)
                {
                    return currentSize;
                }
            }

            var height = currentSize.Height;
            var width = currentSize.Width;
            var widthScale = minSize.Width / width;
            var heightScale = minSize.Height / height;

            // 如果超过最大的大小
            var maxWidthScale = maxSize.Width / width;
            var maxHeightScale = maxSize.Height / height;

            var maxScale = Math.Min(maxWidthScale, maxHeightScale);
            var minScale = Math.Max(widthScale, heightScale);
            minScale = Math.Max(minScale, 1.0);
            var scale = Math.Min(minScale, maxScale);
            // 尽可能使用整数
            scale = Math.Ceiling(scale);
            return new Size(width * scale, height * scale);
        }
    }

    public class Size
    {
        public Size(double width, double height)
        {
            Width = width;
            Height = height;
        }

        public double Width { set; get; }
        public double Height { set; get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Width={Width:0.00} Height={Height:0.00}";
        }
    }
}