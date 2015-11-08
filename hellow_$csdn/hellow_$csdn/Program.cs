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
            hellow h = new hellow();
            h.ce();
        }
    }
    class hellow
    {
        public hellow()
        {

        }
        public void ce()
        {
            var csdn = "csdn";
            var result = $"Hello {csdn}";
            Console.Write(result);
        }
    }
}
