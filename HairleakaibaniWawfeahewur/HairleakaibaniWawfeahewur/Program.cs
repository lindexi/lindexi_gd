using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using stakx.WIC;

namespace HairleakaibaniWawfeahewur
{
    class GifPlayer
    {
        public GifPlayer(string filePath)
        {
            WICImagingFactory factory = WicImagingFactory = new WICImagingFactory();
            IWICBitmapDecoder gifDecoder = WicBitmapDecoder = factory.CreateDecoderFromFilename(filePath,
                null, // Do not prefer a particular vendor
                WICDecodeOptions.WICDecodeMetadataCacheOnDemand);
            var wicMetadataQueryReader = gifDecoder.GetMetadataQueryReader();
            GetBackgroundColor(wicMetadataQueryReader);

            var propertyVariant = new PROPVARIANT();
            wicMetadataQueryReader.GetMetadataByName("/logscrdesc/Width", ref propertyVariant);
            if ((propertyVariant.Type & VARTYPE.VT_UI2) == VARTYPE.VT_UI2)
            {
                Width = propertyVariant.Value.UI2;
            }

            // 清空一下
            propertyVariant = new PROPVARIANT();
            wicMetadataQueryReader.GetMetadataByName("/logscrdesc/Height", ref propertyVariant);
            if ((propertyVariant.Type & VARTYPE.VT_UI2) == VARTYPE.VT_UI2)
            {
                Height = propertyVariant.Value.UI2;
            }

            // 清空一下
            propertyVariant = new PROPVARIANT();
            wicMetadataQueryReader.GetMetadataByName("/logscrdesc/PixelAspectRatio", ref propertyVariant);
            if ((propertyVariant.Type & VARTYPE.VT_UI1) == VARTYPE.VT_UI1)
            {
                var pixelAspRatio = propertyVariant.Value.UI2;
                if (pixelAspRatio != 0)
                {
                    // Need to calculate the ratio. The value in uPixelAspRatio 
                    // allows specifying widest pixel 4:1 to the tallest pixel of 
                    // 1:4 in increments of 1/64th
                    float pixelAspRatioFloat = (pixelAspRatio + 15F) / 64F;
                    // Calculate the image width and height in pixel based on the
                    // pixel aspect ratio. Only shrink the image.

                    if (pixelAspRatioFloat > 1.0F)
                    {
                        WidthGifImagePixel = Width;
                        HeightGifImagePixel = (ushort)(Height / pixelAspRatioFloat);
                    }
                    else
                    {
                        WidthGifImagePixel = (ushort)(Width * pixelAspRatioFloat);
                        HeightGifImagePixel = Height;
                    }
                }
                else
                {
                    // The value is 0, so its ratio is 1
                    WidthGifImagePixel = Width;
                    HeightGifImagePixel = Height;
                }
            }


            var frameCount = gifDecoder.GetFrameCount();
            for (int i = 0; i < frameCount; i++)
            {
                var wicBitmapFrameDecode = gifDecoder.GetFrame(i);
                var wicFormatConverter = factory.CreateFormatConverter();
                wicFormatConverter.Initialize(wicBitmapFrameDecode, WICPixelFormat.WICPixelFormat24bppBGR, WICBitmapDitherType.WICBitmapDitherTypeNone, null, 0, WICBitmapPaletteType.WICBitmapPaletteTypeCustom);

                const int bytesPerPixel = 3;// BGR 格式
                var size = wicFormatConverter.GetSize();
                var imageByte = new byte[size.Width * size.Height * bytesPerPixel];
                wicFormatConverter.CopyPixels(24, imageByte);

            }
        }

        private ushort WidthGifImagePixel { get; }
        private ushort HeightGifImagePixel { get; }

        private ushort Width { get; }
        private ushort Height { get; }

        private IWICBitmapDecoder WicBitmapDecoder { get; }
        private WICImagingFactory WicImagingFactory { get; }

        private void GetBackgroundColor(IWICMetadataQueryReader wicMetadataQueryReader)
        {
            // 如果图片里面包含了 global palette 就需要获取 palette 和背景色
            var propertyVariant = new PROPVARIANT();
            wicMetadataQueryReader.GetMetadataByName("/logscrdesc/GlobalColorTableFlag", ref propertyVariant);

            byte backgroundIndex = 0;

            var globalPalette = (propertyVariant.Type & VARTYPE.VT_BOOL) == VARTYPE.VT_BOOL &&
                                propertyVariant.Value.UI1 > 0;
            if (globalPalette)
            {
                propertyVariant = new PROPVARIANT();
                wicMetadataQueryReader.GetMetadataByName("/logscrdesc/BackgroundColorIndex", ref propertyVariant);

                if ((propertyVariant.Type & VARTYPE.VT_UI1) == VARTYPE.VT_UI1)
                {
                    backgroundIndex = propertyVariant.Value.UI1;
                }

                var wicPalette = WicImagingFactory.CreatePalette();
                WicBitmapDecoder.CopyPalette(wicPalette);

            }

        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var gifPlayer = new GifPlayer("1.gif");


            //var encoderInfo = EnumEncoders(factory)
            //    .First(temp => temp.GetFriendlyName() == "PNG Encoder");



            //foreach (var text in wicMetadataQueryReader.GetEnumerator().AsEnumerable())
            //{
            //    Console.WriteLine(text);
            //}


            //var frameCount = gifDecoder.GetFrameCount();

            //for (int i = 0; i < frameCount; i++)
            //{
            //    var wicBitmapFrameDecode = gifDecoder.GetFrame(i);
            //    wicBitmapFrameDecode.GetSize(out var width, out var height);
            //    var stride = 8;
            //    byte[] image = new byte[width * height* stride];
            //    wicBitmapFrameDecode.CopyPixels(1, image);

            //    var encoder = factory.CreateEncoder(encoderInfo.GetContainerFormat());
            //    using (var stream = File.Create($"{i}.png"))
            //    {
            //        encoder.Initialize(stream.AsCOMStream(), WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache);
            //        var frame = encoder.CreateNewFrame();
            //        frame.Initialize(null);
            //        var format = wicBitmapFrameDecode.GetPixelFormat();
            //        frame.SetPixelFormat(ref format);

            //        frame.SetResolution(new Resolution(96, 96));
            //        frame.SetSize(width, height);

            //        //IWICBitmapFrameEncodeExtensions.WritePixels(frame, height, width * bytesPerPixel, image);

            //        frame.Commit();
            //        encoder.Commit();
            //    }
            //}


        }



        static IEnumerable<IWICBitmapEncoderInfo> EnumEncoders(IWICImagingFactory wic)
        {
            return wic.CreateComponentEnumerator(WICComponentType.WICEncoder, WICComponentEnumerateOptions.WICComponentEnumerateDefault)
                .AsEnumerable()
                .OfType<IWICBitmapEncoderInfo>();
        }
    }
}
