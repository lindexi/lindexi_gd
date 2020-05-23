using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.Number;

namespace HaifeljiweajeYeelarkerjairere
{
    class Program
    {
        static void Main(string[] args)
        {
            TestRecognizeNumber("我挖了两个坑");

            TestRecognizeNumber("我挖了两个坑，埋了两千个小伙伴");

            TestRecognizeNumber("在2020的时候，我挖了两个坑，埋了两千个小伙伴，其中一百三十个小伙伴是逗比");
        }

        private static void TestRecognizeNumber(string text)
        {
            var recognizeChineseNumber = NumberRecognizer.RecognizeNumber(text, Culture.Chinese);
            Console.WriteLine(text);
            Console.WriteLine(ModelResultToString(recognizeChineseNumber));
        }

        private static string ModelResultToString(List<ModelResult> list)
        {
            var pre = "\t";
            var breakLine = "\r\n";
            var str = new StringBuilder();
            foreach (var modelResult in list)
            {
                str.Append(pre)
                    .Append("关键词： ")
                    .Append(modelResult.Text)
                    .Append(breakLine)
                    .Append(pre)
                    .Append($"起点 {modelResult.Start} 终点 {modelResult.End}")
                    .Append(breakLine);
                if (modelResult.Resolution.TryGetValue("value", out var value))
                {
                    str.Append(pre)
                        .Append("值：")
                        .Append(value)
                        .Append(breakLine);
                }

                str.Append(breakLine);
            }

            return str.ToString();
        }
    }
}