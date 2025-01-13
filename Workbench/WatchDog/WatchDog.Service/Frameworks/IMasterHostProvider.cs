using WatchDog.Service.Contexts;

namespace WatchDog.Service.Frameworks;

/// <summary>
/// 用于获取主设备信息
/// </summary>
public interface IMasterHostProvider
{
    /// <summary>
    /// 获取主设备信息
    /// </summary>
    /// <returns></returns>
    Task<MasterHostResult> GetMasterHostAsync();
}