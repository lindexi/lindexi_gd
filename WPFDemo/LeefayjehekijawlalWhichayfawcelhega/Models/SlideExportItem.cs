namespace LeefayjehekijawlalWhichayfawcelhega.Models;

internal sealed class SlideExportItem
{
    public required int PageNumber { get; init; }

    public required byte[] Content { get; init; }

    public required string FileExtension { get; init; }
}
