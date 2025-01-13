namespace WatchDog.Service.Frameworks;

/// <summary>
/// 假的设备信息提供者
/// </summary>
public class FakeSelfHostProvider : ISelfHostProvider
{
    public FakeSelfHostProvider()
    {
        _hostName = "Fake-" + Random.Shared.Next().ToString();
    }

    private readonly string _hostName;

    public Task<string> GetSelfHostAsync()
    {
        return Task.FromResult(_hostName);
    }
}