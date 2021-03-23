using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LemjelnunebeBerjojikur
{
    class Program
    {
        static void Main(string[] args)
        {
            var wordFileInfoList = new List<WordFileInfo>();
            var file = @"word list.txt";
            var wordList = File.ReadAllLines(file);
            foreach (var wordFileInfo in wordList)
            {
                var textList = wordFileInfo.Split("|");
                if (textList.Length == 2)
                {
                    wordFileInfoList.Add(new WordFileInfo(textList[0], textList[1]));
                }
            }

            var json = JsonSerializer.Serialize(wordFileInfoList);
            File.WriteAllText(@"word list.json", json);
        }
    }

    public class WordFileInfo
    {
        public WordFileInfo(string word, string fileName)
        {
            Word = word;
            FileName = fileName;
        }

        public string Word { get; }

        public string FileName { get; }
    }
}
