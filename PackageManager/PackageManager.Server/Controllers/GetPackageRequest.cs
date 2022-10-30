namespace PackageManager.Server.Controllers;

public record GetPackageRequest(string? ClientVersion,string PackageId)
{
}

public record GetPackageResponse(string Message);