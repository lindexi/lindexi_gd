namespace MiniMaxSdk.Images.Models;

/// <summary>
/// 表示 MiniMax 文生图接口返回的一张图片。
/// </summary>
/// <param name="Url">当返回格式为 <c>url</c> 时的图片链接。</param>
/// <param name="Bytes">当返回格式为 <c>base64</c> 时解码得到的图片二进制内容。</param>
/// <param name="SuggestedFileExtension">根据图片内容或链接推断得到的建议文件扩展名。</param>
public sealed record MiniMaxGeneratedImage(string? Url, byte[]? Bytes, string SuggestedFileExtension)
{
    /// <summary>
    /// 获取当前图片是否包含二进制内容。
    /// </summary>
    public bool HasBinaryContent => Bytes is { Length: > 0 };

    /// <summary>
    /// 将当前图片保存到指定文件。
    /// </summary>
    /// <param name="outputFile">输出文件。</param>
    /// <param name="cancellationToken">用于取消异步操作的取消令牌。</param>
    /// <returns>表示异步保存操作的任务。</returns>
    public async Task SaveAsync(FileInfo outputFile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outputFile);

        if (!HasBinaryContent || Bytes is null)
        {
            throw new InvalidOperationException("当前图片不包含可保存的二进制内容。请在请求时使用 base64 返回格式。");
        }

        if (outputFile.Directory is DirectoryInfo directory)
        {
            directory.Create();
        }

        await File.WriteAllBytesAsync(outputFile.FullName, Bytes, cancellationToken).ConfigureAwait(false);
    }
}