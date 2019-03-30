
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;

namespace RaiwairwofayfuHeehenagelki.GifImage
{
    /// <summary>
    ///     GIF图像文件的解码器
    /// </summary>
    internal class GifDecoder
    {
        #region 对gif图像文件进行解码

        /// <summary>
        ///     对gif图像文件进行解码
        /// </summary>
        /// <param name="gifPath">gif文件路径</param>
        internal static GifImage Decode(string gifPath)
        {
            FileStream fs = null;
            StreamHelper streamHelper = null;
            var gifImage = new GifImage();
            var graphics = new List<GraphicEx>();
            var frameCount = 0;
            try
            {
                fs = new FileStream(gifPath, FileMode.Open);
                streamHelper = new StreamHelper(fs);
                //读取文件头
                gifImage.Header = streamHelper.ReadString(6);
                //读取逻辑屏幕标示符
                gifImage.LogicalScreenDescriptor = streamHelper.GetLCD(fs);
                if (gifImage.LogicalScreenDescriptor.GlobalColorTableFlag)
                {
                    //读取全局颜色列表
                    gifImage.GlobalColorTable =
                        streamHelper.ReadByte(gifImage.LogicalScreenDescriptor.GlobalColorTableSize * 3);
                }

                var nextFlag = streamHelper.Read();
                while (nextFlag != 0)
                {
                    if (nextFlag == GifExtensions.ImageLabel)
                    {
                        ReadImage(streamHelper, fs, gifImage, graphics, frameCount);
                        frameCount++;
                    }
                    else if (nextFlag == GifExtensions.ExtensionIntroducer)
                    {
                        var gcl = streamHelper.Read();
                        switch (gcl)
                        {
                            case GifExtensions.GraphicControlLabel:
                            {
                                var graphicEx = streamHelper.GetGraphicControlExtension(fs);
                                graphics.Add(graphicEx);
                                break;
                            }
                            case GifExtensions.CommentLabel:
                            {
                                var comment = streamHelper.GetCommentEx(fs);
                                gifImage.CommentExtensions.Add(comment);
                                break;
                            }
                            case GifExtensions.ApplicationExtensionLabel:
                            {
                                var applicationEx = streamHelper.GetApplicationEx(fs);
                                gifImage.ApplictionExtensions.Add(applicationEx);
                                break;
                            }
                            case GifExtensions.PlainTextLabel:
                            {
                                var textEx = streamHelper.GetPlainTextEx(fs);
                                gifImage.PlainTextExtensions.Add(textEx);
                                break;
                            }
                        }
                    }
                    else if (nextFlag == GifExtensions.EndIntroducer)
                    {
                        //到了文件尾
                        break;
                    }

                    nextFlag = streamHelper.Read();
                }
            }
            finally
            {
                fs.Close();
            }

            return gifImage;
        }

        #endregion

        #region 私有方法，用于类内部使用     

        private static void ReadImage(StreamHelper streamHelper, Stream fs, GifImage gifImage, List<GraphicEx> graphics,
            int frameCount)
        {
            var imgDes = streamHelper.GetImageDescriptor(fs);
            var frame = new GifFrame();
            frame.ImageDescriptor = imgDes;
            frame.LocalColorTable = gifImage.GlobalColorTable;
            if (imgDes.LctFlag)
            {
                frame.LocalColorTable = streamHelper.ReadByte(imgDes.LctSize * 3);
            }

            var lzwDecoder = new LZWDecoder(fs);
            var dataSize = streamHelper.Read();
            frame.ColorDepth = dataSize;
            var piexel = lzwDecoder.DecodeImageData(imgDes.Width, imgDes.Height, dataSize);
            frame.IndexedPixel = piexel;
            var blockSize = streamHelper.Read();
            var data = new DataStruct(blockSize, fs);
            var graphicEx = graphics[frameCount];
            frame.GraphicExtension = graphicEx;
            frame.FearjallgarhifarFecheakabeli = GetImageFromPixel(piexel, imgDes.Width, imgDes.Height, frame.Palette,
                imgDes.InterlaceFlag);
            gifImage.Frames.Add(frame);
        }

        private static ImageSource GetImageFromPixel(byte[] pixel,int iw,
            int ih, Color[] colorTable, bool interlactFlag)
        {
            var dest = new int[iw * ih];
            var pointer = 0;
            var tempPointer = pointer;

            var offSet = 0;
            //var i = 0;
            if (interlactFlag)
                {
                    #region 交织存储模式

                    var i = 0;
                    var pass = 0; //当前通道            
                    while (pass < 4)
                    {
                        //总共有4个通道
                        if (pass == 1)
                        {
                            pointer = tempPointer;
                            pointer += 4 * iw;
                            offSet += 4 * iw;
                        }
                        else if (pass == 2)
                        {
                            pointer = tempPointer;
                            pointer += 2 * iw;
                            offSet += 2 * iw;
                        }
                        else if (pass == 3)
                        {
                            pointer = tempPointer;
                            pointer += 1 * iw;
                            offSet += 1 * iw;
                        }

                        var rate = 2;
                        if ((pass == 0) | (pass == 1))
                        {
                            rate = 8;
                        }
                        else if (pass == 2)
                        {
                            rate = 4;
                        }

                        while (i < pixel.Length)
                        {
                            dest[pointer] = colorTable[pixel[i++]].ToBgra32();
                            pointer++;

                            offSet++;
                            if (i % iw == 0)
                            {
                                pointer += iw * (rate - 1);
                                offSet += iw * (rate - 1);
                                if (offSet >= pixel.Length)
                                {
                                    pass++;
                                    offSet = 0;
                                    break;
                                }
                            }
                        }
                    }

                    #endregion
                }
                else
                {
                    for (var i = 0; i < pixel.Length;)
                    {
                        dest[pointer]= colorTable[pixel[i++]].ToBgra32();;
                        pointer++;
                    }
                }

           return BitmapSource.Create(iw, ih, 96, 96, PixelFormats.Bgr32, null, dest, 4 * iw);
        }

   
        #endregion
    }

    internal static class ColorToBgra
    {
        public static int ToBgra32(this Color color)
        {
            int c = color.B;
            c <<= 8;
            c += color.G;
            c <<= 8;
            c += color.R;
            c <<= 8;
            c += color.A;
            return c;
        }
    }

}