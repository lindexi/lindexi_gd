using System;
using System.Collections.Generic;
using System.Linq;

namespace MacListConvert
{
    class Program
    {
        static void Main(string[] args)
        {
            var text = "2:123:45:67;123:123:123:123:123";

            List<string> macList = new List<string>();

            // 先使用;分号分割

            var decimalMacTextList = text.Split(';');
            foreach (var decimalMacText in decimalMacTextList)
            {
                // 使用:分割然后转换进制
                var decimalTextList = decimalMacText.Split(':');

                List<int> decimalList = new List<int>();
                foreach (var decimalText in decimalTextList)
                {
                    decimalList.Add(int.Parse(decimalText));
                }

                macList.Add(string.Join(':', decimalList.Select(temp => temp.ToString("X2"))));
            }

            foreach (var mac in macList)
            {
                Console.WriteLine(mac);
            }
        }
    }
}
