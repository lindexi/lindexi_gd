using PptxGenerator.Models;

namespace PptxGenerator.Evaluation;

/// <summary>
/// SlideML 评估器接口，对生成的 SlideML 及其渲染结果进行质量评估。
/// </summary>
public interface ISlideEvaluator
{
    /// <summary>
    /// 评估 SlideML 生成结果的质量。
    /// </summary>
    /// <param name="userPrompt">用户原始需求文本。</param>
    /// <param name="slideXml">生成的 SlideML XML。</param>
    /// <param name="renderedXml">渲染回填后的 XML。</param>
    /// <param name="warnings">渲染警告文本。</param>
    /// <param name="previewImageBytes">渲染预览图的 PNG 字节数据，可为 <see langword="null"/>。</param>
    /// <param name="originalScreenshot">原始 PPT 截图，用于还原度对比评估，可为 <see langword="null"/>。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>评估结果。</returns>
    Task<SlideEvaluationResult> EvaluateAsync(
        string userPrompt,
        string slideXml,
        string renderedXml,
        string warnings,
        byte[]? previewImageBytes,
        IPreviewImage? originalScreenshot = null,
        CancellationToken cancellationToken = default);
}
