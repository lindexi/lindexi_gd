using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hellow__csdn
{
    class Program
    {
        static void Main(string[] args)
        {
            //hellow h = new hellow();
            //h.ce();
            
            string csdn = "csdn";
            double n = 1.1315;
            string str = $"Hello {csdn} 新特性";
            Console.WriteLine(str);

            str = $"Hello {csdn} 新特性 {n}";
            Console.WriteLine(str);

            str = $"Hello {csdn} 新特性 {n:1.##}";
            Console.WriteLine(str);

            //不生效
            str = $"Hello {csdn:10} 新特性 {n:1.##}";
            Console.WriteLine(str);

            str = $"Hello {csdn,10} 新特性 {n:1.##}";
            Console.WriteLine(str);

            str = $"Hello {csdn.PadRight(10)} 新特性 {n:1.##}";
            Console.WriteLine(str);

            str = $"Hello {(csdn =="csdn"?"csdn":"lindexi")} 新特性 {n:1.##}";
            Console.WriteLine(str);
        }
    }
    class hellow
    {
        public hellow()
        {

        }
        public void ce()
        {
            string csdn = "csdn";
            string result = $"Hello {csdn}";
            Console.Write(result);
        }
    }
}
