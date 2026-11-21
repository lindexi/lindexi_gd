namespace XiaoXiIme.Cli;

internal sealed class Distribution(string root)
{
    internal string Root { get; } = Path.GetFullPath(root);
    internal string Launcher => Path.Combine(Root, "XiaoXiIme.exe");
    internal string TestHost => Path.Combine(Root, "Tests", "XiaoXiIme.IntegrationTestHost.exe");
    internal string Ime(string rid) => Path.Combine(Root, "native", "ime", rid, "XiaoXiIme.ime");
    internal string Tsf(string rid) => Path.Combine(Root, "native", "tsf", rid, "XiaoXiIme.TsfModule.dll");
    internal string TsfProbe(string rid) => Path.Combine(Root, "Tests", "tsf", rid, "XiaoXiIme.TsfAbiHost.exe");

    internal static Distribution Open(string? directory) =>
        new(directory ?? Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory);

    internal string? MissingInstallFile() => new[] { Launcher, Ime("win-x64"), Ime("win-x86") }
        .FirstOrDefault(path => !File.Exists(path));
}
