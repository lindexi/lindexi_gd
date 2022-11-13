using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PackageManager.Server.Context;
using PackageManager.Server.Model;
using PackageManager.Server.Utils;

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
        if (!string.IsNullOrEmpty(request?.PackageIdOrNamePattern))
        {
            // 判断最新版本的是否支持
            // 当前的客户端版本大于等于最低支持客户端版本
            var clientVersionValue = long.MaxValue;
            if (Version.TryParse(request.ClientVersion, out var clientVersion))
            {
                clientVersionValue = clientVersion.VersionToLong();
            }

            var packageInfo = await FindPackageInfoAsync(request.PackageIdOrNamePattern, clientVersionValue);

            if (packageInfo != null)
            {
                return Ok(new GetPackageResponse("Success", packageInfo));
            }
            else
            {
                // 尝试进入遍历的方式，传入的也许是名字
                // 但是有些不愿意作为可见的，那还是不给好了，只允许采用包 Id 的方式提供
                // 于是这里就不再处理
            }
        }

        return Ok(new GetPackageResponse($"NotFound {request}", null));
    }

    /// <summary>
    /// 获取所有的包的更新
    /// </summary>
    /// <param name="updateAllPackageRequest"></param>
    /// <returns></returns>
    [HttpPost(nameof(UpdateAllPackage))]
    public async Task<IActionResult> UpdateAllPackage(UpdateAllPackageRequest updateAllPackageRequest)
    {
        var clientVersionValue = 0L;
        if (Version.TryParse(updateAllPackageRequest.ClientVersion, out var clientVersion))
        {
            clientVersionValue = clientVersion.VersionToLong();
        }

        // 其实客户端版本，更多的是在客户端版本
        // 服务端只是一个判断
        if (clientVersionValue <= 0)
        {
            return Ok(new UpdateAllPackageResponse(ResponseErrorCode.DoNotSupportClientVersion.Code,
                ResponseErrorCode.DoNotSupportClientVersion.Message, new List<PackageInfo>(0)));
        }

        var result = new List<PackageInfo>();
        foreach (var updatePackageRequest in updateAllPackageRequest.PackageList ?? new List<UpdatePackageRequest>(0))
        {
            // 似乎一条条性能有点差
            var packageInfo = await FindPackageInfoAsync(updatePackageRequest.PackageId, clientVersionValue);

            if (packageInfo is not null)
            {
                if (packageInfo.Version > updatePackageRequest.CurrentPackageVersion)
                {
                    result.Add(packageInfo);
                }
            }
        }

        return Ok(new UpdateAllPackageResponse(ResponseErrorCode.Ok.Code, ResponseErrorCode.Ok.Message, result));
    }

    /// <summary>
    /// 列举首页的所有包
    /// </summary>
    /// <returns></returns>
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
            var currentPackageInfo =
                await PackageManagerContext.LatestPackageDbSet.FirstOrDefaultAsync(t => t.PackageId == packageId);
            if (currentPackageInfo != null)
            {
                PackageManagerContext.LatestPackageDbSet.Remove(currentPackageInfo);
            }

            var latestPackageInfo = new LatestPackageInfo();
            request.PackageInfo.CopyTo(latestPackageInfo);
            PackageManagerContext.LatestPackageDbSet.Add(latestPackageInfo);

            var storagePackageInfo = new StoragePackageInfo();
            request.PackageInfo.CopyTo(storagePackageInfo);
            PackageManagerContext.PackageStorageDbSet.Add(storagePackageInfo);
            await PackageManagerContext.SaveChangesAsync();
            return Ok();
        }

        return NotFound();
    }

    private async Task<PackageInfo?> FindPackageInfoAsync(string packageId, long clientVersion)
    {
        var packageInfo = await
            PackageManagerContext.LatestPackageDbSet.FirstOrDefaultAsync(t => t.PackageId == packageId);

        if (packageInfo != null)
        {
            // 判断一下客户端版本

            // 判断最新版本的是否支持
            // 当前的客户端版本大于等于最低支持客户端版本

            if (clientVersion >= packageInfo.SupportMinClientVersion)
            {
                return packageInfo;
            }
        }

        // 否则，判断一下历史版本
        var storagePackageInfo = await PackageManagerContext.PackageStorageDbSet
            .Where(t => t.PackageId == packageId)
            .Where(t => clientVersion >= t.SupportMinClientVersion).OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync();

        return storagePackageInfo;
    }
}

public record PutPackageRequest(PackageInfo PackageInfo);

public record UpdateAllPackageResponse(int Code, string Message, List<PackageInfo> PackageList);

public record UpdateAllPackageRequest(List<UpdatePackageRequest> PackageList, string ClientVersion)
{
}

public record UpdatePackageRequest(string PackageId, long CurrentPackageVersion);

public static class ResponseErrorCode
{
    public static ErrorCode Ok => new ErrorCode(0, "OK");
    public static ErrorCode DoNotSupportClientVersion => new ErrorCode(1000, "不支持此客户端版本");
}

public readonly record struct ErrorCode(int Code, string Message);

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