using System;
using lindexi.src;

namespace CumuvelBaysiLaySomorvu
{
    class Program
    {
        static void Main(string[] args)
        {
            var qpush = new Qpush("lindexi","23");
            qpush.PushMessageAsync("测试");
        }
    }
}
