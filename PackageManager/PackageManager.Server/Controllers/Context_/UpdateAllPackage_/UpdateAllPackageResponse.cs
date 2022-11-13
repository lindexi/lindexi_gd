using PackageManager.Server.Model;

namespace PackageManager.Server.Controllers;

public record UpdateAllPackageResponse(int Code, string Message, List<PackageInfo> PackageList);