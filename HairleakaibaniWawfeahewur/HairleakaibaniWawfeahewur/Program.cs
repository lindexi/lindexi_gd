using System;
using System.Collections.Generic;
using System.Linq;
using stakx.WIC;

namespace HairleakaibaniWawfeahewur
{
    class Program
    {
        static void Main(string[] args)
        {
            WICImagingFactory factory = new WICImagingFactory();

            foreach (var wicBitmapEncoderInfo in EnumEncoders(factory))
            {
                Console.WriteLine(wicBitmapEncoderInfo.GetFriendlyName());
            }
        }

        static IEnumerable<IWICBitmapEncoderInfo> EnumEncoders(IWICImagingFactory wic)
        {
            return wic.CreateComponentEnumerator(WICComponentType.WICEncoder, WICComponentEnumerateOptions.WICComponentEnumerateDefault)
                .AsEnumerable()
                .OfType<IWICBitmapEncoderInfo>();
        }
    }
}
