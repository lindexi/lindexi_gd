namespace PackageManager.Server.Controllers;

public record GetPackageRequest(string? ClientVersion)
{
}

public record GetPackageResponse(string Message);