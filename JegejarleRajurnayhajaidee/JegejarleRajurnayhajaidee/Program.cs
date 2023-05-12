using Microsoft.Win32;

using System.Runtime.CompilerServices;
using System.Security;
using stakx.WIC;


namespace JegejarleRajurnayhajaidee;

internal class Program
{
    static void Main(string[] args)
    {
        WICImagingFactory factory = new WICImagingFactory();
        var componentEnumerator = factory.CreateComponentEnumerator(WICComponentType.WICDecoder,WICComponentEnumerateOptions.WICComponentEnumerateDefault);

        foreach (var o in componentEnumerator.AsEnumerable())
        {
            IWICBitmapCodecInfo codecInfo = o as IWICBitmapCodecInfo;
            if (codecInfo != null)
            {
                Console.WriteLine("----------");
                Console.WriteLine($"CLSID: {codecInfo.GetCLSID()}");
                Console.WriteLine(codecInfo.GetFriendlyName());
                Console.WriteLine($"FileExtensions: {string.Join(";", codecInfo.GetFileExtensions())}");
                Console.WriteLine($"MimeType: {string.Join(";", codecInfo.GetMimeTypes())}");
                Console.WriteLine($"Version: {codecInfo.GetVersion()}");
                Console.WriteLine("----------");
            }
        }
    }
}