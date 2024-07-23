using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;

namespace KurwalljurchawrallKakeawelba;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        var stringBuilder = new StringBuilder();

        var stopwatch = Stopwatch.StartNew();
        //var compositeFormat = CompositeFormat.Parse("{0}");
        var cache = new List<char>(7); // 1000_0000

        for (int i = 0; i < 1000_0000; i++)
        {
            stringBuilder.Clear();
            stringBuilder.Append(i);
            //stringBuilder.AppendFormat(CultureInfo.InvariantCulture, compositeFormat, i);

            //var number = i;

            //cache.Clear();
            //while (number > 0)
            //{
            //    cache.Add((char) ((number % 10) + '0'));

            //    number /= 10;
            //}

            //for (var cacheIndex = cache.Count - 1; cacheIndex >= 0; cacheIndex--)
            //{
            //    stringBuilder.Append(cache[cacheIndex]);
            //}
        }
        stopwatch.Stop();
    }
}

