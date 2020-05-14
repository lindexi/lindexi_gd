using System;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace CallnernawbawceKairwemwhejeene
{
    class Program
    {
        static void Main(string[] args)
        {
            // 119
            CharUnicodeRange.CheckSort();

            // CjkCompatibility
            Console.WriteLine(CharUnicodeRange.GetUnicodeRangeName((char)0x3300));
        }
    }
}