namespace Builder;

internal static class RuntimeLayout
{
    internal static Dictionary<string, string> Validate(string root, string arch)
    {
        foreach (string forbidden in new[] { "sdk", "packs", "templates", "sdk-manifests" })
            if (Directory.Exists(Path.Combine(root, forbidden)))
                throw new InvalidOperationException(Text.Missing("runtime-only: " + forbidden));
        string frameworkRoot = Path.Combine(root, "shared", "Microsoft.NETCore.App");
        if (!Directory.Exists(frameworkRoot)) throw new InvalidOperationException(Text.Missing(frameworkRoot));
        string framework = Directory.GetDirectories(frameworkRoot).Single();
        string version = Path.GetFileName(framework);
        string fxr = Path.Combine(root, "host", "fxr", version, "hostfxr.dll");
        string muxer = Path.Combine(root, "dotnet.exe");
        foreach (string path in new[] { muxer, fxr, Path.Combine(root, "LICENSE.txt"), Path.Combine(root, "ThirdPartyNotices.txt") }
                     .Concat(new[] { "System.Private.CoreLib.dll", "System.Runtime.dll", "System.Console.dll", "coreclr.dll", "clrjit.dll", "hostpolicy.dll",
                         "Microsoft.NETCore.App.deps.json", "Microsoft.NETCore.App.runtimeconfig.json" }.Select(name => Path.Combine(framework, name))))
            if (!File.Exists(path)) throw new InvalidOperationException(Text.Missing(path));
        foreach (string native in new[] { muxer, fxr, Path.Combine(framework, "coreclr.dll"), Path.Combine(framework, "clrjit.dll"), Path.Combine(framework, "hostpolicy.dll") })
            if (!BinaryInspection.Inspect(native, arch).HeaderMatchesTarget)
                throw new InvalidOperationException(Text.Get("Pe"));
        return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .ToDictionary(path => Path.GetRelativePath(root, path), BinaryInspection.Hash);
    }
}
