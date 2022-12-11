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
        // 这个方法的作用其实有两个：
        // 1. 对其他的 IConfigurationProvider 的结果进行过滤
        // 2. 返回给框架层，此 IConfigurationProvider 提供的配置项

        // 自己测试：
        // 1. 什么都不做，返回的是 earlierKeys 的内容
        // 2. 直接返回 Array.Empty<string>();
        // 3. 拼接出新的列表，如 

        // 例如这个类型提供的配置里面，包含的是 Foo.F1=123; Foo.F2=123; Foo.F3=123 三个值内容
        // 传入的 父路径(parentPath) 是叫做 `Foo` 那么就应该将 `Foo.F1` 和 `Foo.F2` 和 `Foo.F3` 三个 Key 项合并 earlierKeys 进行返回
        // 默认都是采用 Concat(earlierKeys) 的方式进行返回的
        // 那什么情况下不是采用直接 Concat(earlierKeys) 的方式？嗯，需要过滤掉，或者是需要进行重新排序

        if (string.IsNullOrEmpty(parentPath))
        {
            /*
            [WTTSTDIO, C:\Program Files (x86)\Windows Kits\10\Hardware Lab Kit\Studio\]
            [windir, C:\Windows]
            ...
            [APPDATA, C:\Users\lindexi\AppData\Roaming]
            [ALLUSERSPROFILE, C:\ProgramData]
            [AllowedHosts, *]
            [, ]
            [:ASPNETCORE_BROWSER_TOOLS, true]
            [:Foo.F3, ]
            [:Foo.F2, ]
            [:Foo.F1, ]
            [Foo.F3, ]
            [Foo.F2, ]
            [Foo.F1, ]
             */
            return new string[] { "Foo.F1", "Foo.F2", "Foo.F3" }.Concat(earlierKeys);

            /*
          [Foo.F3, ]
          [Foo.F2, ]
          [Foo.F1, ]
          [WTTSTDIO, C:\Program Files (x86)\Windows Kits\10\Hardware Lab Kit\Studio\]
          [windir, C:\Windows]
          ...
          [APPDATA, C:\Users\lindexi\AppData\Roaming]
          [ALLUSERSPROFILE, C:\ProgramData]
          [AllowedHosts, *]
          [, ]
          [:Foo.F3, ]
          [:Foo.F2, ]
          [:Foo.F1, ]
          [:ASPNETCORE_BROWSER_TOOLS, true]
           */
            return earlierKeys.Concat(new string[] { "Foo.F1", "Foo.F2", "Foo.F3" });
          
        }

        return earlierKeys;

        // 此时枚举是空白
        return Array.Empty<string>();
    }
}