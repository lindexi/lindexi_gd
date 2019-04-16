using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace DerewewebaGineyinereburha
{
    /// <summary>
    /// 存放
    /// </summary>
    class HilerehanuwereleQeyifinu
    {
        public void Draw(HahicafojuwembaKuleajigaideeba hahicafojuwembaKuleajigaideeba)
        {
            int px = 5;
            int py = 5;

            int width = (int) hahicafojuwembaKuleajigaideeba.Width * px;
            int height = (int) hahicafojuwembaKuleajigaideeba.Height * py;

            //var buffer = new byte[width * height * 4];

            //for (int i = 0; i < hahicafojuwembaKuleajigaideeba.Width; i++)
            //{
            //    for (int j = 0; j < hahicafojuwembaKuleajigaideeba.Height; j++)
            //    {
            //        for (int x = px * i; x < px * (i + 1); x++)
            //        {
            //            for (int y = py * j; y < py * (j + 1); y++)
            //            {
            //                buffer[y * width + x * 4 + 0] = 100;
            //                buffer[y * width + x * 4 + 1] = 0;
            //                buffer[y * width + x * 4 + 2] = 0;
            //                buffer[y * width + x * 4 + 3] = 100;
            //            }
            //        }
            //    }
            //}

            //var memoryStream = new MemoryStream(buffer);
            //using (memoryStream)
            //{
            //    var bitmap = new Bitmap(width,height,width,PixelFormat.Format32bppArgb, Marshal.UnsafeAddrOfPinnedArrayElement(buffer,0));
            //    bitmap.Save("F:\\temp\\1.png", ImageFormat.Png);
            //}


            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            //var bitmapdata = bitmap.LockBits(new Rectangle(new Point(), new Size(width, height)), ImageLockMode.WriteOnly,
            //     PixelFormat.Format32bppArgb);

            var color = Color.Black;

            //Span<uint> image=new Span<uint>(bitmapdata.Scan0, width* height);

            for (int i = 0; i < hahicafojuwembaKuleajigaideeba.Width; i++)
            {
                for (int j = 0; j < hahicafojuwembaKuleajigaideeba.Height; j++)
                {
                    if (hahicafojuwembaKuleajigaideeba.World[i * j])
                    {
                        for (int x = px * i; x < px * (i + 1); x++)
                        {
                            for (int y = py * j; y < py * (j + 1); y++)
                            {
                                bitmap.SetPixel(x, y, color);
                            }
                        }
                    }
                 
                }
            }

            //bitmap.UnlockBits(bitmapdata);

            bitmap.Save("F:\\temp\\1.png", ImageFormat.Png);
        }

        public void Storage(HahicafojuwembaKuleajigaideeba hahicafojuwembaKuleajigaideeba)
        {

        }

        public void Read()
        {

        }
    }
}