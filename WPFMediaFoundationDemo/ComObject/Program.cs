using Lsj.Util.Win32.BaseTypes;
using Lsj.Util.Win32.ComInterfaces;
using Lsj.Util.Win32.Extensions.COM;
using System;

namespace ComObject
{

    class Program
    {
        static void Main(string[] args)
        {
            var iUnknown = TestGetIUnknown();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();


            var hResult = iUnknown.Value.QueryInterface(IIDs.IID_IUnknown, out var obj);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var ret = iUnknown.Value.Release();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            ret = iUnknown.Value.Release();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            ret = iUnknown.Value.AddRef();
        }

        static P<IUnknown> TestGetIUnknown()
        {
            var testObj = new BaseComObject<IUnknown>();
            return testObj.GetNativePointer();
        }
    }
}