namespace WatchDog.Service.Frameworks;

/// <summary>
/// 用于检查主设备是否可用
/// </summary>
public interface IMasterHostStatusChecker
{
    /// <summary>
    /// 检查主设备是否可用
    /// </summary>
    /// <param name="host"></param>
    /// <returns></returns>
    Task<bool> CheckMasterHostEnableAsync(string host);
}