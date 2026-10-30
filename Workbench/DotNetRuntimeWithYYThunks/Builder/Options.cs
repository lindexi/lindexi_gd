using System.Resources;

namespace Builder;

internal static class Text
{
    private static readonly ResourceManager Resources = new("Builder.Strings", typeof(Text).Assembly);
    internal static string Get(string key) => Resources.GetString(key) ?? key;
    internal static string Missing(string value) => string.Format(Get("Missing"), value);
}

internal sealed record Options(string Command, string Arch, string TargetOs, string Runtime, string Yy,
    string Obj, string? File, string Configuration, int Timeout, bool DryRun, string Root, bool ConfigureOnly, bool FullRuntime)
{
    internal static Options Parse(string[] args, string root)
    {
        if (args.Length == 0 || !new[] { "doctor", "plan", "baseline", "build", "inspect" }.Contains(args[0]))
            throw new ArgumentException(Text.Get("Arguments"));
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        bool dry = false, configureOnly = false, fullRuntime = false;
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--full-runtime" && !fullRuntime) { fullRuntime = true; continue; }
            if (args[i] == "--dry-run" && !dry) { dry = true; continue; }
            if (args[i] == "--configure-only" && !configureOnly) { configureOnly = true; continue; }
            if (!new[] { "--target-os", "--arch", "--runtime", "--yy", "--obj", "--file", "--configuration", "--timeout" }.Contains(args[i]) ||
                i + 1 == args.Length || !values.TryAdd(args[i], args[++i]))
                throw new ArgumentException(Text.Get("Arguments"));
        }
        string Get(string key, string fallback = "") => values.GetValueOrDefault(key, fallback);
        string arch = Get("--arch"), os = Get("--target-os");
        if (arch is not ("x64" or "x86") || os is not ("win7" or "vista" or "xp"))
            throw new ArgumentException(Text.Get("Arguments"));
        if (os != "win7") throw new NotSupportedException(Text.Get("Unsupported"));
        string config = Get("--configuration", "Release");
        if (config is not ("Release" or "Debug" or "Checked") ||
            !int.TryParse(Get("--timeout", "3600"), out int timeout) || timeout is < 1 or > 86400)
            throw new ArgumentException(Text.Get("Arguments"));
        string PathValue(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.Any(c => char.IsControl(c) || "\";%!$`[]&|<>^()".Contains(c)))
                throw new ArgumentException(Text.Get("Arguments"));
            return Path.GetFullPath(path);
        }
        string yy = PathValue(Get("--yy", Path.Combine(root, "..", "YY-Thunks")));
        return new(args[0], arch, os, PathValue(Get("--runtime", Path.Combine(root, "..", "runtime"))), yy,
            PathValue(Get("--obj", Path.Combine(yy, "objs", arch, "YY_Thunks_for_Win7.obj"))),
            values.ContainsKey("--file") ? PathValue(Get("--file")) : null, config, timeout, dry, root, configureOnly, fullRuntime);
    }
}
