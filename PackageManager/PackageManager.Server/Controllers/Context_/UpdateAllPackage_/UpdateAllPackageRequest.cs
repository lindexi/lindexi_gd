namespace PackageManager.Server.Controllers;

public record UpdateAllPackageRequest(List<UpdatePackageRequest> PackageList, string ClientVersion)
{
}