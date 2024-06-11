using System.ComponentModel.DataAnnotations;

namespace UsingHardLinkToZipNtfsDiskSize;

public class FileRecordModel
{
    [Key]
    [Required]
    public string FilePath { set; get; } = null!;

    [Required]
    public long FileLength { set; get; }

    [Required]
    public string FileSha1Hash { set; get; } = null!;
}