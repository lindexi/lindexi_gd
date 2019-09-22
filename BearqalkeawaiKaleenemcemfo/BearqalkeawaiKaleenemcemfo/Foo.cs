using System;
using System.Runtime.InteropServices;

namespace BearqalkeawaiKaleenemcemfo
{
    [ComVisible(true)]
    [Guid("5742D257-CCCC-4F7A-8191-6362609C458D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IFoo
    {
        /// <summary>
        /// 有趣方法
        /// </summary>
        /// <returns></returns>
        string Foo();
    }

    [ComVisible(true)]
    [Guid("5742D257-CCCC-4F7A-8191-6362609C458D")]
    public class Foo : IFoo
    {
        /// <inheritdoc />
        string IFoo.Foo()
        {
            return "林德熙是逗比";
        }
    }
}