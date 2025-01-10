using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TextVisionComparer;

public class VisionComparer
{
    /// <summary>
    /// 比较两张图片的相似度
    /// </summary>
    /// <param name="textImageFile1"></param>
    /// <param name="textImageFile2"></param>
    /// <returns></returns>
    public VisionCompareResult Compare(FileInfo textImageFile1, FileInfo textImageFile2)
    {
        // 对比算法：
        // 1. 逐个像素点对比，如果不同则记录下来
        // 2. 计算不同的像素点数量
        // 3. 计算不同的像素点的范围列表
        // 优势：
        // 文本很多都是像素边缘差异，通过范围列表可以减少误差
        using Image<Rgba32> image1 = Image.Load<Rgba32>(textImageFile1.FullName);
        using Image<Rgba32> image2 = Image.Load<Rgba32>(textImageFile2.FullName);

        //if (image1.Size != image2.Size)
        //{
        //    return VisionCompareResult.FailResult($"两张图片尺寸不相同，不能进行相似比较");
        //}

        // 总像素点数量
        int imageWidth = Math.Min(image1.Width, image2.Width);
        int imageHeight = Math.Min(image1.Height, image2.Height);
        var pixelCount = imageWidth * imageHeight;
        // 不同的像素点数量
        int dissimilarPixelCount = 0;

        double totalDistanceValue = 0;

        double[] pixelDistance = new double[pixelCount];

        Parallel.For(0, pixelCount, n =>
        {
            // ReSharper disable AccessToDisposedClosure
            var y = n / imageWidth;
            var x = n % imageWidth;

            Rgba32 pixel1 = image1[x, y];
            Rgba32 pixel2 = image2[x, y];

            if (pixel1 == pixel2)
            {
                pixelDistance[n] = 0;
                return;
            }

            double distance =
                (Math.Abs(pixel1.A - pixel2.A))
                + (Math.Abs(pixel1.R - pixel2.R))
                + (Math.Abs(pixel1.G - pixel2.G))
                + (Math.Abs(pixel1.B - pixel2.B));
            pixelDistance[n] = distance;
        });

        var mapArray = new BitArray(pixelCount, false);

        for (int i = 0; i < pixelDistance.Length; i++)
        {
            double distance = pixelDistance[i];
            totalDistanceValue += distance;

            if (distance > 0)
            {
                dissimilarPixelCount++;
                mapArray[i] = true;
            }
        }

        List<VisionCompareRect> list = GetVisionCompareRectList(mapArray, imageWidth, imageHeight);

        double similarityValue = totalDistanceValue / imageWidth * imageHeight;
        return new VisionCompareResult(true, similarityValue, pixelCount, dissimilarPixelCount, list, "成功");
    }

    private static List<VisionCompareRect> GetVisionCompareRectList(BitArray mapArray, int imageWidth, int imageHeight)
    {
        var pixelCount = imageWidth * imageHeight;

        var list = new List<VisionCompareRect>();
        // 尝试寻找有多少范围是存在不相似的
        var ignoreArray = new BitArray(pixelCount, false);
        for (var i = 0; i < mapArray.Count; i++)
        {
            if (ignoreArray[i] is true)
            {
                // 被访问过了
                continue;
            }

            if (mapArray[i] is false)
            {
                continue;
            }

            var y = i / imageWidth;
            var x = i % imageWidth;

            // 向下右扩展
            // 处理不了空白范围，或者是溶解效果的差异
            var minBottom = GetMaxBottom(x, y);
            var maxRight = x;

            for (int currentX = x; currentX < imageWidth; currentX++)
            {
                var index = y * imageWidth + currentX;
                if (mapArray[index] is false)
                {
                    break;
                }

                if (minBottom == y)
                {
                    // 不需要继续找了，只找最小的
                }
                else
                {
                    var bottom = GetMaxBottom(currentX, y);
                    minBottom = Math.Min(minBottom, bottom);
                }

                maxRight = currentX;
            }

            if (maxRight > x || minBottom > y)
            {
                VisionCompareRect visionCompareRect = new VisionCompareRect(x, y, maxRight, minBottom);
                list.Add(visionCompareRect);

                // 添加标记，减少重复访问
                for (int currentY = y; currentY <= minBottom; currentY++)
                {
                    for (int currentX = x; currentX <= maxRight; currentX++)
                    {
                        var index = currentY * imageWidth + currentX;
                        ignoreArray[index] = true;
                    }
                }
            }

            int GetMaxBottom(int initX, int initY)
            {
                int bottom = initY;
                for (int currentY = initY; currentY < imageHeight; currentY++)
                {
                    var index = currentY * imageWidth + initX;
                    if (mapArray[index] is false)
                    {
                        break;
                    }

                    bottom = currentY;
                }

                return bottom;
            }
        }

        return list;
    }
}
