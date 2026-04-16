using System.IO;

using LeefayjehekijawlalWhichayfawcelhega.Models;

namespace LeefayjehekijawlalWhichayfawcelhega.Services;

internal sealed class ImageExportService
{
    public async Task<string> ExportAsync(IReadOnlyList<SlideExportItem> slideExportItems, string exportRootDirectory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(slideExportItems);

        if (slideExportItems.Count == 0)
        {
            throw new ArgumentException("没有可导出的图片。", nameof(slideExportItems));
        }

        if (string.IsNullOrWhiteSpace(exportRootDirectory))
        {
            throw new ArgumentException("请先设置导出目录。", nameof(exportRootDirectory));
        }

        Directory.CreateDirectory(exportRootDirectory);

        string exportDirectory = Path.Combine(
            exportRootDirectory,
            DateTime.Now.ToString("yyyyMMdd-HHmmss"));

        Directory.CreateDirectory(exportDirectory);

        foreach (SlideExportItem slideExportItem in slideExportItems.OrderBy(item => item.PageNumber))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string fileName = $"{slideExportItem.PageNumber:D2}{slideExportItem.FileExtension}";
            string filePath = Path.Combine(exportDirectory, fileName);
            await File.WriteAllBytesAsync(filePath, slideExportItem.Content, cancellationToken);
        }

        return exportDirectory;
    }
}
