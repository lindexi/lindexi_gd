namespace MathGraphs.Neural;

/// <summary>归档根目录内的已发布训练检查点管理，不处理临时文件、配置及实验报告。</summary>
public static class CheckpointArchive
{
    /// <summary>按最后写入时间选择最新检查点；无存档返回 null。加载错误由调用方报告，不静默重训。</summary>
    public static string? FindLatest(string archiveRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archiveRoot);
        return Files(archiveRoot).FirstOrDefault()?.FullName;
    }

    internal static void RetainLatest(string archiveRoot, int maximum, string publishedPath)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximum);
        var published = Path.GetFullPath(publishedPath);
        var others = Files(archiveRoot).Where(f => !string.Equals(f.FullName, published, StringComparison.OrdinalIgnoreCase));
        foreach (var file in others.Skip(maximum - 1)) file.Delete();
    }

    private static IEnumerable<FileInfo> Files(string root) => !Directory.Exists(root) ? [] :
        Directory.EnumerateFiles(root, "checkpoint-*.json", SearchOption.AllDirectories).Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ThenByDescending(file => file.Name, StringComparer.Ordinal)
            .ThenByDescending(file => file.FullName, StringComparer.Ordinal);
}
