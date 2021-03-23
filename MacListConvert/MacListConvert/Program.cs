using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;

namespace MacListConvert
{
    class Program
    {
        static void Main(string[] args)
        {
            var csvFile = "1.csv";

            十六进制Mac地址转十进制(csvFile);
            return;

            if (args.Length > 0)
            {
                csvFile = args[0];
            }

            csvFile = Path.GetFullPath(csvFile);
            Console.WriteLine($"进行转换的 CSV 文件:{csvFile}");

            var output = "2.csv";
            var stringBuilder = new StringBuilder();

            var csvFileTextLine = File.ReadLines(csvFile).ToList();
            var regex = new Regex(@"""([\d:]*)""(,""[\d-]*"",""[\d:]*"")");
            for (var i = 1; i < csvFileTextLine.Count; i++)
            {
                var textLine = csvFileTextLine[i];
                var match = regex.Match(textLine);
                if (match.Success)
                {
                    var macListText = match.Groups[1].Value;
                    var macList = ConvertMacList(macListText);
                    foreach (var mac in macList)
                    {
                        //var macInfo = new MacInfo()
                        //{
                        //    Mac = mac,
                        //    Number= match.Groups[2].Value
                        //};

                        //csvWriter.WriteRecord(macInfo);
                        stringBuilder.Append($"\"{mac}\"{match.Groups[2].Value}\r\n");
                    }
                }
            }

            //csvWriter.Dispose();
            File.WriteAllText(output, stringBuilder.ToString());
        }

        private static void 十六进制Mac地址转十进制(string filePath)
        {
            var textLineList = File.ReadAllLines(filePath);
            List<string> macList = new List<string>();

            foreach (var textLine in textLineList)
            {
                if (string.IsNullOrEmpty(textLine))
                {
                    continue;
                }

                var macTextList = textLine.Split(':');

                List<int> decimalList = new List<int>();
                foreach (var macText in macTextList)
                {
                    decimalList.Add(Convert.ToInt32(macText, 16));
                }

                macList.Add(string.Join(':', decimalList.Select(temp => temp.ToString())));
            }

            File.WriteAllLines("2.csv", macList);
        }

        private static List<string> ConvertMacList(string text)
        {
            List<string> macList = new List<string>();

            if (string.IsNullOrEmpty(text))
            {
                return macList;
            }

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

            return macList;
        }
    }
}
