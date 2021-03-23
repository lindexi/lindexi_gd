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

            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            var color = Color.Black;

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