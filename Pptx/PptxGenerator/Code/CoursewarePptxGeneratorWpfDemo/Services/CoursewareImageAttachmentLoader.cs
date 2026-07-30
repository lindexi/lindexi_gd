using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.AI;
using PptxGenerator;
using CoursewarePptxGeneratorWpfDemo.Resources;
using CoursewarePptxGeneratorWpfDemo.ViewModels;

namespace CoursewarePptxGeneratorWpfDemo.Services;

internal interface ICoursewareImageAttachmentLoader
{
    Task<IReadOnlyList<DataContent>> LoadAsync(
        IReadOnlyList<CoursewareChatImageAttachmentViewModel> attachments,
        CancellationToken cancellationToken);
}

internal sealed class CoursewareImageAttachmentLoader : ICoursewareImageAttachmentLoader
{
    private static readonly IReadOnlyDictionary<string, string> ExtensionMediaTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".bmp"] = "image/bmp",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp",
        };

    private readonly WpfDispatcher _dispatcher;

    internal CoursewareImageAttachmentLoader()
        : this(WpfDispatcher.Instance)
    {
    }

    internal CoursewareImageAttachmentLoader(WpfDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        _dispatcher = dispatcher;
    }

    public async Task<IReadOnlyList<DataContent>> LoadAsync(
        IReadOnlyList<CoursewareChatImageAttachmentViewModel> attachments,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(attachments);
        var contents = new List<DataContent>(attachments.Count);
        foreach (var attachment in attachments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var bytes = await ReadAllBytesAsync(attachment.FullName, cancellationToken).ConfigureAwait(false);
                var mediaType = await ValidateAndGetMediaTypeAsync(
                    bytes,
                    Path.GetExtension(attachment.FullName),
                    cancellationToken).ConfigureAwait(false);
                contents.Add(new DataContent(bytes, mediaType));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new CoursewareImageAttachmentLoadException(
                    attachment,
                    string.Format(
                        System.Globalization.CultureInfo.CurrentCulture,
                        CoursewareUiStrings.ImageAttachmentLoadFailedFormat,
                        attachment.DisplayName),
                    exception);
            }
        }

        return contents;
    }

    private static async Task<byte[]> ReadAllBytesAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var memoryStream = fileStream.Length is > 0 and <= int.MaxValue
            ? new MemoryStream((int) fileStream.Length)
            : new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, 81920, cancellationToken).ConfigureAwait(false);
        return memoryStream.ToArray();
    }

    private Task<string> ValidateAndGetMediaTypeAsync(
        byte[] bytes,
        string extension,
        CancellationToken cancellationToken)
    {
        return _dispatcher.InvokeAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var stream = new MemoryStream(bytes, writable: false);
            var decoder = BitmapDecoder.Create(
                stream,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);
            if (decoder.Frames.Count == 0)
            {
                throw new InvalidOperationException(CoursewareUiStrings.ImageAttachmentHasNoFrame);
            }

            var mediaType = GetDecoderMediaType(decoder);
            if (mediaType is null && !ExtensionMediaTypes.TryGetValue(extension, out mediaType))
            {
                throw new NotSupportedException(CoursewareUiStrings.ImageAttachmentTypeUnsupported);
            }

            return Task.FromResult(mediaType);
        });
    }

    private static string? GetDecoderMediaType(BitmapDecoder decoder)
    {
        var mimeTypes = decoder.CodecInfo?.MimeTypes;
        if (string.IsNullOrWhiteSpace(mimeTypes))
        {
            return null;
        }

        return mimeTypes
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(mimeType => mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase));
    }
}

internal sealed class CoursewareImageAttachmentLoadException : System.Exception
{
    internal CoursewareImageAttachmentLoadException(
        CoursewareChatImageAttachmentViewModel attachment,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        Attachment = attachment;
    }

    internal CoursewareChatImageAttachmentViewModel Attachment { get; }
}
