#nullable disable

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace PackageManager.Server.Model;

[Index(nameof(PackageId))]
public class PackageInfo
{
    [JsonIgnore]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { set; get; }

    public string PackageId { set; get; }

    /// <summary>
    /// 人类友好的插件名
    /// </summary>
    public string Name { set; get; }

    public string Author { set; get; }
    public long Version { set; get; }
    public string Description { set; get; }

    /// <summary>
    /// 图标的下载地址
    /// </summary>
    public string IconUrl { set; get; }

    /// <summary>
    /// 是否能够展示出来
    /// </summary>
    public bool CanShow { set; get; }

    /// <summary>
    /// 描述信息，可不要太长哦
    /// </summary>
    public string DownloadUrl { set; get; }

    public long SupportMinClientVersion { set; get; }
}