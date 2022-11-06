using Microsoft.EntityFrameworkCore;
using PackageManager.Server.Model;

using System.ComponentModel.DataAnnotations.Schema;

namespace PackageManager.Server.Context;

public class PackageManagerContext:DbContext
{
    public PackageManagerContext(DbContextOptions options) : base(options)
    {
    }

    /// <summary>
    /// 只包含最新版本的信息
    /// </summary>
    public DbSet<LatestPackageInfo> LatestPackageDbSet { set; get; } 
    // 框架注入
        = null!;

    /// <summary>
    /// 包含历史版本的
    /// </summary>
    public DbSet<StoragePackageInfo> PackageStorageDbSet { set; get; }
    // 框架注入
        = null!;
}

public class LatestPackageInfo : PackageInfo
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { set; get; }
}

public class StoragePackageInfo : PackageInfo
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { set; get; }
}
