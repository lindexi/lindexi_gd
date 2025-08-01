namespace TextVisionComparer;

public readonly record struct VisionCompareResult(bool Success, double SimilarityValue, int PixelCount, int DissimilarPixelCount, IReadOnlyList<VisionCompareRect> CompareRectList, string DebugReason)
{
    public static VisionCompareResult FailResult(string debugReason) => new VisionCompareResult(false, 0, 0, 0, null!, debugReason);

    /// <summary>
    /// 是不是相似
    /// </summary>
    /// <returns></returns>
    public bool IsSimilar()
    {
        if (!Success)
        {
            // 没有比较的意义
            return false;
        }

        if ((double) DissimilarPixelCount / PixelCount > 0.1)
        {
            return false;
        }

        foreach (VisionCompareRect visionCompareRect in CompareRectList)
        {
            if (visionCompareRect.Width > 5 && visionCompareRect.Height > 5)
            {
                return false;
            }

            var area = visionCompareRect.Width * visionCompareRect.Height;
            if (area > 20)
            {
                return false;
            }
        }

        return true;
    }
}
