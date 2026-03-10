// See https://aka.ms/new-console-template for more information

if (OperatingSystem.IsLinux())
{
    var inputFolder = @"/sys/class/input";

    foreach (var subFolder in Directory.EnumerateDirectories(inputFolder))
    {
        var idFolder = Path.Join(subFolder, $"device/id");
        var vendor = await TryReadAsync(Path.Join(idFolder, "vendor"));
        var product = await TryReadAsync(Path.Join(idFolder, "product"));
        var version = await TryReadAsync(Path.Join(idFolder, "version"));
        Console.WriteLine($"{subFolder}:");
        Console.WriteLine($"Vendor: {vendor}, Product: {product}, Version: {version}");
        Console.WriteLine();

        async ValueTask<string?> TryReadAsync(string filePath)
        {
            if (File.Exists(filePath))
            {
                var text = await File.ReadAllTextAsync(filePath);
                text = text.Trim();
                return text;
            }
            else
            {
                return null;
            }
        }
    }
}

Console.WriteLine("Hello, World!");
