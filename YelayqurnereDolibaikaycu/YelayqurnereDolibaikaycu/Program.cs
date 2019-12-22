using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YelayqurnereDolibaikaycu
{
    class Program
    {
        static void Main(string[] args)
        {
            Do();

            // 其他业务
            for (int i = 0; i < int.MaxValue; i++)
            {
                Console.WriteLine("欢迎访问我博客 https://blog.lindexi.com 里面有大量 UWP WPF 博客");
                Task.Delay(TimeSpan.FromSeconds(10));
            }
        }

        private static void Do()
        {
            var business = new Business();
            business.Do();
        }

        public static event Action Foo;
    }

    class Business
    {
        public Business()
        {
            NumberList = new BusinessNumber(1000);
        }

        public void Do()
        {
            Program.Foo += Do;
        }

        public BusinessNumber NumberList { get; }
    }

    class BusinessNumber : List<int>
    {
        public BusinessNumber(int capacity) : base(capacity)
        {
        }
    }
}
