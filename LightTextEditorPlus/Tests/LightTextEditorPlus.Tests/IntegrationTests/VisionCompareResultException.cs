using TextVisionComparer;

namespace LightTextEditorPlus.Tests.IntegrationTests;

public class VisionCompareResultException(string name, VisionCompareResult result, string assertImageFilePath, string imageFilePath) : Exception
{
    public override string ToString()
    {
        var debugReason = result.Success ? "" : $"\r\n对比的调试原因:{result.DebugReason}";

        var maxSize = 0;
        foreach (VisionCompareRect visionCompareRect in result.CompareRectList)
        {
            var size = visionCompareRect.Width * visionCompareRect.Height;
            maxSize = Math.Max(size, maxSize);
        }

        return $"""
                图片视觉对比失败
                测试用例: {name}
                对比成功: {result.Success}{debugReason}
                视觉相似:{result.IsSimilar()}
                视觉相似度:{result.SimilarityValue}
                不相似的像素数量:{result.DissimilarPixelCount}
                最大不相似区域大小:{maxSize}
                像素数量:{result.PixelCount}
                预设图片:{assertImageFilePath}
                当前状态截图:{imageFilePath}
                """;
    }
}