using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WicallbachercalLaicheljaihawwhallbem;

public class FileStorageModel
{
    [Key]
    [Required]
    public string FileSha1Hash { set; get; } = null!;

    /// <summary>
    /// 原始的文件路径
    /// </summary>
    [Required]
    public string OriginFilePath { set; get; } = null!;

    /// <summary>
    /// 有多少个文件引用了
    /// </summary>
    public long ReferenceCount { set; get; }

    /// <summary>
    /// 文件大小
    /// </summary>
    public long FileLength { set; get; }
}