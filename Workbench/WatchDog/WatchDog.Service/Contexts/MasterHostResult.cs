namespace WatchDog.Service.Contexts;

/// <summary>
/// 获取主机信息结果
/// </summary>
/// <param name="SelfIsMaster">当前是否就是主设备</param>
/// <param name="MasterHost">主设备地址</param>
public record MasterHostResult(bool SelfIsMaster, string MasterHost);