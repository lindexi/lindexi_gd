using System.Reflection;

namespace DotNetCampus.Installer.Lib.Hosts.Contexts;

public readonly record struct AssemblyManifestResourceInfo(Assembly Assembly, string ManifestResourceName)
{
    public Stream GetManifestResourceStream()
    {
        var resourceInfo = this;
        Stream? assetsStream = resourceInfo.Assembly.GetManifestResourceStream(resourceInfo.ManifestResourceName);
        if (assetsStream is null)
        {
            throw new ArgumentException($"传入的 ManifestResourceName={resourceInfo.ManifestResourceName} 找不到资源。可能是忘记嵌入资源，也可能是改了名字忘记改这里，也可能传错程序集。 Assembly={resourceInfo.Assembly.FullName}");
        }

        return assetsStream;
    }
};