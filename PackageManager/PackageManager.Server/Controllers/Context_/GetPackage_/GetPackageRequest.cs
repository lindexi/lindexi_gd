using PackageManager.Server.Model;

namespace PackageManager.Server.Controllers;

/// <summary>
/// 获取包的请求
/// </summary>
/// <param name="ClientVersion"></param>
/// <param name="PackageIdOrNamePattern">包的 Id 号，或者是名字</param>
public record GetPackageRequest(string? ClientVersion, string PackageIdOrNamePattern)
{
}

public record GetPackageResponse(string Message, PackageInfo? PackageInfo)
{
    public bool IsNotFound => PackageInfo is null;
}