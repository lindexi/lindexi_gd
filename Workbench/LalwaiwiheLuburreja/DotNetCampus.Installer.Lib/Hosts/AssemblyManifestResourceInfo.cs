using System.Reflection;

namespace DotNetCampus.Installer.Lib.Hosts;

public readonly record struct AssemblyManifestResourceInfo(Assembly Assembly, string ManifestResourceName);