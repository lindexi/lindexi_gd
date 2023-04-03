using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace TextShapingExample
{
    class Program
    {
        [DllImport("TextShaping.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateTextShaping();

        [DllImport("TextShaping.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void DestroyTextShaping(IntPtr textShaping);

        [DllImport("TextShaping.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint GetWordBreaks(IntPtr textShaping, string fontFilePath, string text);

        static void Main(string[] args)
        {
            string fontFilePath = @"C:\Windows\Fonts\Arial.ttf"; // 示例字体文件路径
            string text = "Hello, world!"; // 待拆分的字符串文本

            IntPtr textShaping = CreateTextShaping();

            uint wordBreakCount = GetWordBreaks(textShaping, fontFilePath, text);
            var wordBreaks = new List<int>();
            for (uint i = 0; i < wordBreakCount; i++)
            {
                wordBreaks.Add((int) Marshal.ReadIntPtr(textShaping, (int) (i * 4)).ToInt64());
            }

            DestroyTextShaping(textShaping);

            Console.WriteLine("单词拆分结果：");
            if (wordBreaks.Count > 0)
            {
                int startIndex = 0;
                for (int i = 0; i < wordBreaks.Count; i++)
                {
                    int endIndex = wordBreaks[i] + 1;
                    string word = text.Substring(startIndex, endIndex - startIndex);
                    startIndex = endIndex;
                    Console.WriteLine(word);
                }
            }
            else
            {
                Console.WriteLine(text);
            }
        }
    }
}