using System.IO;
using System.Windows.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

using LeefayjehekijawlalWhichayfawcelhega.Models;

namespace LeefayjehekijawlalWhichayfawcelhega.ViewModels;

public sealed class ImageCandidateViewModel : ObservableObject
{
    internal ImageCandidateViewModel(GeneratedImageResult generatedImageResult)
    {
        ArgumentNullException.ThrowIfNull(generatedImageResult);

        Index = generatedImageResult.Index;
        FileExtension = generatedImageResult.FileExtension;
        Content = generatedImageResult.Content;
        SourceUrl = generatedImageResult.SourceUrl;
        PreviewImage = CreatePreviewImage(generatedImageResult.Content);
    }

    public int Index { get; }

    public string FileExtension { get; }

    public byte[] Content { get; }

    public string? SourceUrl { get; }

    public BitmapImage PreviewImage { get; }

    public string DisplayName => $"候选图 {Index}";

    private static BitmapImage CreatePreviewImage(byte[] content)
    {
        using MemoryStream memoryStream = new(content);

        BitmapImage bitmapImage = new();
        bitmapImage.BeginInit();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.StreamSource = memoryStream;
        bitmapImage.EndInit();
        bitmapImage.Freeze();
        return bitmapImage;
    }
}
