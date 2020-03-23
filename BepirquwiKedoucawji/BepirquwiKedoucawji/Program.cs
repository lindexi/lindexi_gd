using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace BepirquwiKedoucawji
{
    class Program
    {
        static void Main(string[] args)
        {
            var str = "123123\r123123\n123123\r\n123";
            var newLineList = str.Split('\n', '\r').Select(text => text = text.Replace("\r", ""))
                .ToList();

            newLineList = SplitMultiLines(str);
        }

        private static List<string> SplitMultiLines(string str)
        {
            var lineList = new List<string>();
            var text = new StringBuilder(str.Length);
            for (var i = 0; i < str.Length; i++)
            {
                var c = str[i];
                if (c == '\r')
                {
                    lineList.Add(text.ToString());
                    text.Clear();

                    if (i < str.Length - 1)
                    {
                        if (str[i + 1] == '\n')
                        {
                            i++;
                        }
                    }
                }
                else if (c == '\n')
                {
                    lineList.Add(text.ToString());
                    text.Clear();

                    if (i < str.Length - 1)
                    {
                        if (str[i + 1] == '\r')
                        {
                            i++;
                        }
                    }
                }
                else
                {
                    text.Append(c);
                }
            }

            lineList.Add(text.ToString());
            return lineList;
        }
    }
}