const string originalDirectory = @"f:\temp\HekefadoQerwaljeaneajeja\原始\";
const string packageDirectory = @"f:\temp\HekefadoQerwaljeaneajeja\安装包\";

if (!Directory.Exists(originalDirectory))
{
    Console.Error.WriteLine($"目录不存在：{originalDirectory}");
    return 1;
}

if (!Directory.Exists(packageDirectory))
{
    Console.Error.WriteLine($"目录不存在：{packageDirectory}");
    return 1;
}

var originalFiles = GetRelativeFilePaths(originalDirectory);
var packageFiles = GetRelativeFilePaths(packageDirectory);

foreach (var file in originalFiles.Except(packageFiles).Order(StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine($"- {file}");
}

foreach (var file in packageFiles.Except(originalFiles).Order(StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine($"+ {file}");
}

return 0;

static HashSet<string> GetRelativeFilePaths(string rootDirectory)
{
    return Directory
        .EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories)
        .Select(file => Path.GetRelativePath(rootDirectory, file))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
