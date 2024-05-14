using System;
using System.Collections.Generic;
using System.Text;

namespace CPF.Linux
{
    static class XError
    {
        private static readonly XErrorHandler s_errorHandlerDelegate = Handler;
        public static XErrorEvent LastError;
        static int Handler(IntPtr display, ref XErrorEvent error)
        {
            LastError = error;
            //StringBuilder stringBuilder = new StringBuilder(100);
            //XLib.XGetErrorText(error.display, error.error_code, stringBuilder, stringBuilder.Length);
            //Console.WriteLine("异常:" + stringBuilder.ToString() + " " + error.request_code + ":" + error.error_code);
            return 0;
        }

        public static void ThrowLastError(string desc)
        {
            var err = LastError;
            LastError = new XErrorEvent();
            if (err.error_code == 0)
                throw new Exception(desc);
            throw new Exception(desc + ": " + err.error_code);

        }

        public static void Init()
        {
            XLib.XSetErrorHandler(s_errorHandlerDelegate);
        }
    }
}
