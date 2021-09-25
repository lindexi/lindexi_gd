using System;

namespace WemjallnacaleayayLerelaywhekebelnaw
{
    class Program
    {
        static void Main(string[] args)
        {
            var start = 3;

            string subString = _pathString.Substring(start, _curIndex - start);
            var num1 = System.Convert.ToDouble(subString, _formatProvider);

            try
            {
                start = 2;

                var span = _pathString.AsSpan(start, _curIndex - start);
                var num2 = double.Parse(span, provider: _formatProvider);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static string _pathString = "abc123ab";
        private static int _curIndex = 6;
        private static IFormatProvider _formatProvider = System.Globalization.CultureInfo.InvariantCulture;
    }
}
