using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PackageManager.Server.Context;
using PackageManager.Server.Model;

namespace PackageManager.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class PackageController : ControllerBase
{
    public PackageController(PackageManagerContext packageManagerContext)
    {
        PackageManagerContext = packageManagerContext;
    }

    private PackageManagerContext PackageManagerContext { get; }

    /// <summary>
    /// 获取包下载或更新
    /// </summary>
    /// <param name="request"></param>
    /// <returns>
    /// 返回给定的包的最新版本，最新版本指的是传入的参数所能支持的最新版本
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GetPackageRequest? request)
    {
        if (!string.IsNullOrEmpty(request?.PackageId))
        {
            var packageInfo = await 
                PackageManagerContext.LatestPackageDbSet.FirstOrDefaultAsync(t => t.PackageId == request.PackageId);

            if (packageInfo != null)
            {
                return Ok(new GetPackageResponse("Success", packageInfo));
            }
        }

        return Ok(new GetPackageResponse($"NotFound {request}",null));
    }


    // 获取所有的包的更新

    // 列举首页的所有包

    [HttpGet(nameof(GetPackageListInMainPage))]
    public IActionResult GetPackageListInMainPage()
    {
        var list = PackageManagerContext.LatestPackageDbSet.Where(t => t.CanShow).ToList();
        return Ok(list);
    }

    /// <summary>
    /// 推送包
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Put([FromBody] PutPackageRequest request)
    {
        if (HttpContext.Request.Headers.TryGetValue("Token", out var value)
            // 证明有权限可以推送
            && string.Equals(value.ToString(), TokenConfiguration.Token, StringComparison.Ordinal))
        {
            // 先从 LatestPackageDbSet 里面移除其他的所有的，然后再加上新的
            // 如此就让 LatestPackageDbSet 只存放最新的
            var packageId = request.PackageInfo.PackageId;
            var currentPackageInfo = await PackageManagerContext.LatestPackageDbSet.FirstOrDefaultAsync(t => t.PackageId == packageId);
            if (currentPackageInfo != null)
            {
                PackageManagerContext.LatestPackageDbSet.Remove(currentPackageInfo);
            }

            PackageManagerContext.LatestPackageDbSet.Add(request.PackageInfo);
            PackageManagerContext.PackageStorageDbSet.Add(request.PackageInfo);
            await PackageManagerContext.SaveChangesAsync();
            return Ok();
        }

        return NotFound();
    }
}

public record PutPackageRequest(PackageInfo PackageInfo);

public class StringVersionComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        // 大于 返回1
        // 等于 返回0
        // 小于 返回负数
        if (x is null && y is null) return 0;
        if (x is null && y is not null) return -1;
        if (x is not null && y is null) return 1;

        var xSuccess = Version.TryParse(x, out var versionX);
        var ySuccess = Version.TryParse(y, out var versionY);

        if (!xSuccess && !ySuccess) return 0;
        if (!xSuccess && ySuccess) return -1;
        if (xSuccess && !ySuccess) return 1;

        if (versionX is not null && versionY is not null)
        {
            return versionX.CompareTo(versionY);
        }
        else
        {
            // 理论上不会进入这个逻辑
            return 0;
        }
    }
}