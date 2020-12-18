using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("FalfairlujeWekufemqune,PublicKey=002400000480000094000000060200000024000052534131000400000100010069a9f306e1a644e0576651ebe3ec12a535f55f2268e50da02cfa9b969b6492a80a4c7bf7b17b9edb232fbfc0c6178ea1f5ef58f3d82f25dfa7b6cf02e0bde35f879e45d8af6847fac7c1c1a5e855d915a552aef4f0dc97d4cab25f70524ca74912121a1f2233c96cd501b5efc717d933bf15f23d256aa7cf37b9ce814fd2def1")]


[assembly: InternalsVisibleTo("KicibehemNilaycahikem,PublicKey=002400000480000094000000060200000024000052534131000400000100010069a9f306e1a644e0576651ebe3ec12a535f55f2268e50da02cfa9b969b6492a80a4c7bf7b17b9edb232fbfc0c6178ea1f5ef58f3d82f25dfa7b6cf02e0bde35f879e45d8af6847fac7c1c1a5e855d915a552aef4f0dc97d4cab25f70524ca74912121a1f2233c96cd501b5efc717d933bf15f23d256aa7cf37b9ce814fd2def1")]

namespace HaircajuwalYelkohebaygo
{
    class Program
    {
        static void Main(string[] args)
        {
            var str = "E79C8BE588B0E8BF99E58FA5E8AF9D";

            List<byte> byteList = new List<byte>();
            for (int i = 0; i < str.Length; i+=2)
            {
                var byteStr = str.Substring(i, 2);
                byteList.Add(byte.Parse(byteStr,NumberStyles.AllowHexSpecifier));
            }

            var text = Encoding.UTF8.GetString(byteList.ToArray());

            Console.WriteLine("Hello World!");
        }
    }
}
