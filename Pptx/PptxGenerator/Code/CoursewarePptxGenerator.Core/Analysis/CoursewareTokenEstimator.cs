namespace CoursewarePptxGenerator.Core.Analysis;

/// <summary>
/// Estimates model token usage without depending on a specific analysis protocol.
/// </summary>
public static class CoursewareTokenEstimator
{
    /// <summary>
    /// Conservatively estimates token usage for mixed CJK and ASCII text.
    /// </summary>
    public static int Estimate(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        long estimatedTokenCount = 0;
        var asciiCharacterCount = 0;
        for (var index = 0; index < text.Length; index++)
        {
            if ((index & 4095) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (text[index] <= 0x7F)
            {
                asciiCharacterCount++;
            }
            else
            {
                estimatedTokenCount++;
            }
        }

        estimatedTokenCount += (asciiCharacterCount + 2L) / 3L;
        return estimatedTokenCount >= int.MaxValue ? int.MaxValue : (int) estimatedTokenCount;
    }
}
