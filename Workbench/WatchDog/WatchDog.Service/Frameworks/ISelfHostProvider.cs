namespace WatchDog.Service.Frameworks;

/// <summary>
/// 自己当前设备的主机信息提供者
/// </summary>
public interface ISelfHostProvider
{
    Task<string> GetSelfHostAsync();
}