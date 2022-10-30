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
    public IActionResult Put([FromBody] PutPackageRequest request)
    {
        if (HttpContext.Request.Headers.TryGetValue("Token", out var value) &&
            string.Equals(value.ToString(), TokenConfiguration.Token, StringComparison.Ordinal))
        {
            // 证明有权限可以推送
            PackageManagerContext.LatestPackageDbSet.Add(request.PackageInfo);
            PackageManagerContext.PackageStorageDbSet.Add(request.PackageInfo);
            PackageManagerContext.SaveChangesAsync();
            return Ok();
        }

        return NotFound();
    }
}

public record PutPackageRequest(PackageInfo PackageInfo);