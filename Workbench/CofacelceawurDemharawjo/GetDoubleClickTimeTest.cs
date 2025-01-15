using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace CofacelceawurDemharawjo;

public class GetDoubleClickTimeTest
{
    [Benchmark]
    public void Test()
    {
        /*
           BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4391/23H2/2023Update/SunValley3)
           13th Gen Intel Core i7-13700K, 1 CPU, 24 logical and 16 physical cores
           .NET SDK 9.0.100
             [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
             DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2


           | Method | Mean     | Error    | StdDev   |
           |------- |---------:|---------:|---------:|
           | Test   | 396.2 ns | 14.32 ns | 41.53 ns |
         */
        // 1,000,000 纳秒 = 1毫秒 ms
        _ = GetDoubleClickTime();
    }

    public const string LibraryName = "user32";

    /// <summary>
    /// 获取鼠标双击事件两次点击的时间间隔
    /// </summary>
    /// <returns></returns>
    [DllImport(LibraryName)]
    public static extern uint GetDoubleClickTime();
    /*
       // ntstubs.c

       UINT NtUserGetDoubleClickTime(
           VOID)
       {
           BEGINRECV_SHARED(UINT, 0);

           /*
            * Blow it off if the caller doesn't have the proper access rights. However,
            * allow CSRSS to use this value internally to the server. Note that if the
            * client tries to retrieve this value itself, the access check will
            * function normally.
            * /
           if ((PpiCurrent()->Process != gpepCSRSS) &&
               (!CheckGrantedAccess(PpiCurrent()->amwinsta, WINSTA_READATTRIBUTES))) {
               MSGERROR(0);
           }

           retval = gdtDblClk;

           TRACE("NtUserGetDoubleClickTime");
           ENDRECV_SHARED();
       }

       // globals.c
       UINT gdtDblClk = 500;
     */
}