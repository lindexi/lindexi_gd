using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ChobigarqerKuhawnakayewe
{
    class Program
    {
        static void Main(string[] args)
        {
            // 在 LinemlallledurKaicawkeedaykerewho 项目使用的词典
            var file = "E:\\download\\Oxford Advanced57414条Txt\\Oxford Advanced Learner's Dictionary 8th Edition(mimihuhu)57414条.txt";

            //var text = File.ReadAllText(file);
            var lineList = File.ReadAllLines(file);

             List<string> wordList =new List<string>();

            string word = null;
            for (var i = 0; i < lineList.Length;i++)
            {
                var line = lineList[i];
                word = line;
                wordList.Add(word);

                i++;
                i++;
                if (i< lineList.Length && !string.IsNullOrEmpty(lineList[i]))
                {
                    Debugger.Break();
                }
            }

            File.WriteAllLines("word list.txt", wordList);
        }
    }
}
