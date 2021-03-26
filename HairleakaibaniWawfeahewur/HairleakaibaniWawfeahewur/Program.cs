using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using stakx.WIC;

namespace HairleakaibaniWawfeahewur
{
    class Program
    {
        static void Main(string[] args)
        {
            WICImagingFactory factory = new WICImagingFactory();

            var encoderInfo = EnumEncoders(factory)
                .FirstOrDefault(temp => temp.GetFriendlyName() == "PNG Encoder");

            const int width = 256;
            const int height = 256;
            const int bytesPerPixel = 3;// BGR 格式

            var random = new Random();

            if (encoderInfo != null)
            {
                var encoder = factory.CreateEncoder(encoderInfo.GetContainerFormat());

                using (var stream = File.Create("1.png"))
                {
                    encoder.Initialize(stream.AsCOMStream(),WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache);

                    var frame = encoder.CreateNewFrame();
                    frame.Initialize(null);

                    var format = WICPixelFormat.WICPixelFormat24bppBGR;
                    frame.SetPixelFormat(ref format);

                    frame.SetResolution(new Resolution(96, 96));
                    frame.SetSize(width, height);

                    var image = new byte[width * height * bytesPerPixel];

                    for (int i = 0; i < height; i++)
                    {
                        for (int j = 0; j < width; j++)
                        {
                            image[(i * width + j) * bytesPerPixel + 0] = (byte)random.Next(255);
                            image[(i * width + j) * bytesPerPixel + 1] = (byte)random.Next(255);
                            image[(i * width + j) * bytesPerPixel + 2] = (byte)random.Next(255);
                        }
                    }

                    IWICBitmapFrameEncodeExtensions.WritePixels(frame, height, width * bytesPerPixel, image);

                    frame.Commit();
                    encoder.Commit();
                }
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
