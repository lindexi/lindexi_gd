#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace WibifelqeQolawwaljecili
{
    class Program
    {
        static void Main(string[] args)
        {
            var file1 = new FileInfo("slide1.xml");
            using var fileStream = file1.OpenRead();

            var file2 = new FileInfo("slide2.xml");
            using var fileStream2 = file2.OpenRead();

            var xDocument1 = XDocument.Load(fileStream);
            var xDocument2 = XDocument.Load(fileStream2);

            var xDocument1Root = xDocument1.Root!;
            var xDocument2Root = xDocument2.Root!;

            try
            {
                CheckValue(xDocument1Root, xDocument2Root);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Environment.Exit(-1);
            }
        }

        private static void CheckValue(XElement xElement1, XElement xElement2)
        {
            var elementCount = new Dictionary<XName, int>();

            foreach (var subElement1 in xElement1.Elements())
            {
                XElement subElement2;

                // 要求列表的元素顺序是相同的
                var subElement2List = xElement2.Elements(subElement1.Name).ToList();
                if (subElement2List.Count == 1)
                {
                    subElement2 = subElement2List[0];
                }
                else
                {
                    if (!elementCount.TryGetValue(subElement1.Name, out var count))
                    {
                        count = 0;
                    }

                    if (count >= subElement2List.Count)
                    {
                        subElement2 = null;
                        Throw($"元素包含的子元素数量不同。xElement1.Count={count} ; xElement2.Count={subElement2List.Count}");
                    }

                    subElement2 = subElement2List[count];

                    count++;
                    elementCount[subElement1.Name] = count;
                }

                if (subElement1.HasElements == subElement2.HasElements)
                {
                    if (subElement1.HasElements)
                    {
                        CheckValue(subElement1, subElement2);
                    }
                    else
                    {
                        var value1 = subElement1.Value;
                        var value2 = subElement2.Value;

                        if (double.TryParse(value1, out var n1) && double.TryParse(value2, out var n2))
                        {
                            if (Math.Abs(n1 - n2) > 0.001)
                            {
                                Throw($"元素的值不匹配，分别是 {value1} 和 {value2}");
                            }
                        }
                        else
                        {
                            if (!string.Equals(value1, value2))
                            {
                                Throw($"元素的值不匹配，分别是 {value1} 和 {value2}");
                            }
                        }
                    }
                }
                else
                {
                    Throw($"元素包含的子元素数量不同");
                }

                void Throw(string message)
                {
                    throw new ElementNoMatchException(message, subElement1, subElement2);
                }
            }
        }
    }

    class ElementNoMatchException : ArgumentException
    {
        public ElementNoMatchException(string message, XElement element1, XElement? element2) : base(message)
        {
            Element1 = element1;
            Element2 = element2;

            if (element1 is IXmlLineInfo xmlLineInfo)
            {
                LineNumber = xmlLineInfo.LineNumber;
            }
        }

        public XElement Element1 { get; }
        public XElement? Element2 { get; }

        public int LineNumber { get; }

        public override string ToString()
        {
            return $"ElementName:{Element1.Name};\r\nLineNumber:{LineNumber};\r\n{base.ToString()}";
        }
    }
}
