using Microsoft.Extensions.Primitives;

namespace WemfogerekemhemWeekererewallji;

class ReadonlyCoinConfiguration : IConfigurationSource, IConfigurationProvider
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return this;
    }

    public bool TryGet(string key, out string value)
    {
        value = string.Empty;
        return false;
    }

    public void Set(string key, string value)
    {
        // 不支持
    }

    public IChangeToken GetReloadToken()
    {
        return new CancellationChangeToken(CancellationToken.None);
    }

    public void Load()
    {
        // 不支持
    }

    public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath)
    {
        return Array.Empty<string>();
    }
}