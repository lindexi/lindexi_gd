namespace PackageManager.Server.Controllers;

public record UpdatePackageRequest(string PackageId, long CurrentPackageVersion);