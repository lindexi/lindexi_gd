using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using dotnetCampus.FileDownloader;

namespace KocarnonerefairbeYuyemdearfurcereche
{
    class Program
    {
        static void Main(string[] args)
        {
            //var port = 8080;

            //var server = $"http://172.30.162.24:{port}";
            //var url = server + $"/ImageTest/";
            //var httpClient = new HttpClient();

            //var imageConvertTaskRequest = new ImageConvertTaskRequest()
            //{
            //    ImageUrl = "http://172.20.115.34:5721/image1.emf"
            //};

            //var response = httpClient.PostAsJsonAsync(url, imageConvertTaskRequest).Result;
            //var resizedFile = $"{server}{response.Content.ReadAsStringAsync().Result}";
            //Console.WriteLine(resizedFile);

            //var downloadFile = new FileInfo(Path.GetTempFileName() + ".png");

            //var segmentFileDownloader = new SegmentFileDownloader(resizedFile, downloadFile);

            //segmentFileDownloader.DownloadFileAsync().Wait();

            //downloadFile.Refresh();
            //Console.WriteLine(downloadFile.Length);
            //Process.Start("explorer", downloadFile.FullName);

            var file = "image1.emf";

            int width = 1920;
            int height = 1080;

            var generatedFile = new FileInfo(@"1.png");
            if (generatedFile.Exists)
            {
                generatedFile.Delete();
            }

            ImageOptimization.ConvertEnhancedMetaFileImage(new FileInfo(file), generatedFile, width * height);

            Process.Start("explorer", $"\"{generatedFile.FullName}\"");
        }


    }

    public class ImageConvertTaskRequest
    {
        public string ImageUrl { set; get; } = null!;
    }


    /// <summary>
    /// 图片优化
    /// </summary>
    public static class ImageOptimization
    {
        public static Size GetImageOptimizationSize(Size originSize, Size requestedSize, Size maxSize)
        {
            var maxPixel = maxSize.Width * maxSize.Height;
            var requestedPixel = requestedSize.Width * requestedSize.Height;

            return GetImageOptimizationSize(originSize, maxPixel, requestedPixel);
        }

        private static Size GetImageOptimizationSize(Size originSize, int maxPixel, int requestedPixel)
        {
            var imagePixel = originSize.Width * originSize.Height;
            if (imagePixel <= maxPixel)
            {
                // 是不是太小了，需要缩放

                if (imagePixel < requestedPixel)
                {
                    return GetOptimizationSize(originSize, requestedPixel);
                }
            }
            else
            {
                return GetOptimizationSize(originSize, maxPixel);
            }

            return originSize;

            static Size GetOptimizationSize(Size originSize, double requestedPixel)
            {
                // 定义 Ow 是 originSize.Width
                // 定义 Oh 是 originSize.Height
                // 返回值为 requestedSize 根据 requestedPixel 计算
                // 定义 Rw 是 requestedSize.Width
                // 定义 Rh 是 requestedSize.Height
                // 假定比例不变，于是有
                // Ow / Oh = Rw / Rh 
                // 也就是前后的宽度高度比例不变
                // 上面表达式交换可以等于
                // Ow * Rh = Rw * Oh
                // 而 requestedPixel = requestedSize.Width * requestedSize.Height
                // 定义 P 是 requestedPixel
                // 于是有 Rw = P / Rh
                // 因此 Ow * Rh = Rw * Oh = P / Rh * Oh
                // Ow * Rh = P / Rh * Oh
                // Rh * Rh = P * Oh / Ow
                // 因此 Rh = Math.Sqrt(P * Oh / Ow)
                // 相同方式可以计算 Rw 的值

                var requestedWidth = (int)Math.Sqrt(requestedPixel * originSize.Width / originSize.Height);
                var requestedHeight = (int)Math.Sqrt(requestedPixel * originSize.Height / originSize.Width);

                return new Size(requestedWidth, requestedHeight);
            }
        }

        public static void ConvertEnhancedMetaFileImage(FileInfo originFile, FileInfo generatedFile, int requestedPixel)
        {
            using var image = Image.FromFile(originFile.FullName);

            var size = GetImageOptimizationSize(new Size(image.Width, image.Height), MaxWidth * MaxHeight, requestedPixel);

            var width = size.Width;
            var height = size.Height;

            var resized = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(resized))
            {
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.DrawImage(image, 0, 0, width, height);
                resized.Save(generatedFile.FullName, ImageFormat.Png);
            }
        }

        private const int MaxWidth = 3840; //图片压缩的最大宽度
        private const int MaxHeight = 2160; //图片压缩的最大高度
    }

    /// <summary>
    /// 增强型图形元文件
    /// </summary>
    public static class EnhancedGraphicsMetafile
    {
        /// <summary>
        /// 修复增强型图形元文件
        /// 尝试把他转换为 png 
        /// </summary>
        /// <returns></returns>
        public static void FixEnhancedGraphicsMetafile(FileInfo originFile, FileInfo generatedFile)
        {
            if (TryFixEnhancedGraphicsMetafile(originFile, generatedFile))
            {
                return;
            }

            // 打开增强型图形元文件，然后准备好目标位图路径。
            using (var emf = new Bitmap(originFile.FullName))
            {
                var filename = generatedFile.FullName;

                // 将增强型图形元文件转成位图。
                emf.Save(filename, ImageFormat.Png);
            }
        }

        private static bool TryFixEnhancedGraphicsMetafile(FileInfo originFile, FileInfo generatedFile)
        {
            try
            {
                using (Metafile mf = new Metafile(originFile.FullName))
                {
                    var filename = generatedFile.FullName;

                    MetafileHeader header = mf.GetMetafileHeader();
                    var width = mf.Width;
                    var height = mf.Height;
                    using (Bitmap bitmap = new Bitmap(width, height))
                    {
                        bitmap.SetResolution(header.DpiX, header.DpiY);
                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            g.Clear(Color.Transparent);
                            g.DrawImage(mf, 0, 0);
                        }

                        bitmap.Save(filename, ImageFormat.Png);
                    }
                }

                return true;
            }
            catch
            {
                //I dont know what should do
                return false;
            }
        }

        /// <summary>
        /// 获取新的文件名
        /// </summary>
        /// <param name="file"></param>
        /// <param name="ext"></param>
        /// <returns></returns>
        internal static string GetNewName(string file, string ext)
        {
            var filename = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ext);
            // 但如果碰巧加完跟另一个名字重了，则换 GUID 重试。
            while (File.Exists(filename))
            {
                filename = Path.Combine(Path.GetDirectoryName(file), Guid.NewGuid().ToString("N") + ext);
            }

            return filename;
        }
    }
}
