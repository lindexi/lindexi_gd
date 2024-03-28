using System.ComponentModel.DataAnnotations;

namespace WicallbachercalLaicheljaihawwhallbem;

public class FileRecordModel
{
    [Key]
    [Required]
    public string FilePath { set; get; } = null!;

    [Required]
    public long FileLength { set; get; }
}