#nullable disable

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PackageManager.Server.Context;

namespace PackageManager.Server.Model;

[Index(nameof(PackageId))]
public class PackageInfo
{
    public string PackageId { set; get; }

    /// <summary>
    /// 人类友好的插件名
    /// </summary>
    public string Name { set; get; }

    public string Author { set; get; }
    public long Version { set; get; }

    /// <summary>
    /// 描述信息，可不要太长哦
    /// </summary>
    public string Description { set; get; }

    /// <summary>
    /// 图标的下载地址
    /// </summary>
    public string IconUrl { set; get; }

    /// <summary>
    /// 是否能够展示出来
    /// </summary>
    public bool CanShow { set; get; }


    public string DownloadUrl { set; get; }

    /// <summary>
    /// 需要下架
    /// </summary>
    /// 需要下架时，可以没有下载地址
    public bool ShouldUninstall { set; get; }

    public long SupportMinClientVersion { set; get; }

    public void CopyTo(PackageInfo packageInfo)
    {
        packageInfo.Author = Author;
        packageInfo.Name = Name;
        packageInfo.Version = Version;
        packageInfo.Description = Description;
        packageInfo.IconUrl = IconUrl;
        packageInfo.SupportMinClientVersion = SupportMinClientVersion;
        packageInfo.DownloadUrl = DownloadUrl;
        packageInfo.CanShow = CanShow;
        packageInfo.PackageId = PackageId;
        packageInfo.ShouldUninstall = ShouldUninstall;
    }
}