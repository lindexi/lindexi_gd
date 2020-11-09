using System;
using System.ComponentModel;

namespace BegibaberGawhilofigurwhal
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }

    class Foo
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("请使用 F2 代替")]
        public int F1
        {
            set => F2 = value;
            get => F2;
        }

        public int F2 { set; get; }
    }
}
