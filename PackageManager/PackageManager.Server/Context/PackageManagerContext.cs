using Microsoft.EntityFrameworkCore;
using PackageManager.Server.Model;

namespace PackageManager.Server.Context;

public class PackageManagerContext : DbContext
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