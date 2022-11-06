using PackageManager.Server.Model;

namespace PackageManager.Server.Controllers;

public record GetPackageRequest(string? ClientVersion,string PackageId)
{
}

public record GetPackageResponse(string Message, PackageInfo? PackageInfo)
{
    public bool IsNotFound => PackageInfo is null;
}