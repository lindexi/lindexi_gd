using System;
using System.Runtime.InteropServices;

namespace BearqalkeawaiKaleenemcemfo
{
    [ComVisible(true)]
    [Guid("5742D257-CCCC-4F7A-8191-63626092458D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IFoo
    {
        /// <summary>
        /// 有趣方法
        /// </summary>
        /// <returns></returns>
        int Foo();
    }

    [ComVisible(true)]
    [Guid("5742D257-CCCC-4F7A-8191-63626092458D")]
    public class Foo : IFoo
    {
        /// <inheritdoc />
        int IFoo.Foo()
        {
            return -21376;
        }
    }
}