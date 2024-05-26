using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace JayhallchocojejalNabinojajarher.Devices;

/// <summary>
/// 设备信息
/// </summary>
public class DeviceInfo
{
    /// <summary>
    /// 创建设备信息
    /// </summary>
    /// <param name="description">
    /// 设备描述
    /// </param>
    /// <param name="instanceID">
    /// 设备实例 ID
    /// </param>
    /// <param name="driverKey">
    /// 驱动程序 Key
    /// </param>
    /// <param name="hardwareIds">
    /// 硬件Id列表
    /// </param>
    internal DeviceInfo(string description, string instanceID, string driverKey, IList<string> hardwareIds)
    {
        Description = description;
        InstanceID = instanceID;
        DriverKey = driverKey;
        HardwareIds = hardwareIds;
        ParsingInstanceID();
    }

    /// <summary>
    /// 总线类型
    /// </summary>
    public BusType BusType { get; private set; }

    /// <summary>
    /// 设备描述
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 设备实例 ID
    /// </summary>
    public string InstanceID { get; }


    /// <summary>
    /// 驱动程序 Key
    /// 通过 HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{DriverKey}\ 可以获取到驱动程序相关信息
    /// </summary>
    public string DriverKey { get; }

    /// <summary>
    /// 硬件 ID
    /// </summary>
    public IList<string> HardwareIds { get; }

    /// <summary>
    /// 供应商识别码
    /// 用于 PCI USB 设备
    /// </summary>
    public string? VendorID { get; private set; }

    /// <summary>
    /// 设备识别码
    /// 用于 PCI 设备
    /// </summary>
    public string? DeviceID { get; private set; }

    /// <summary>
    /// 产品识别码
    /// 用于 USB 设备
    /// </summary>
    public string? ProductID { get; private set; }

    private static readonly Regex PciRegex = new Regex(@"^PCI\\VEN_([0-9A-F]{4})&DEV_([0-9A-F]{4})", RegexOptions.None);
    private static readonly Regex UsbRegex = new Regex(@"^USB\\VID_([0-9A-F]{4})&PID_([0-9A-F]{4})", RegexOptions.None);

    private void ParsingInstanceID()
    {
        var matches = PciRegex.Matches(InstanceID);
        if (matches.Count > 0)
        {
            BusType = BusType.PCI;
            var match = matches[0];
            VendorID = match.Groups[1].Value;
            DeviceID = match.Groups[2].Value;
            return;
        }
        matches = UsbRegex.Matches(InstanceID);
        if (matches.Count > 0)
        {
            BusType = BusType.USB;
            var match = matches[0];
            VendorID = match.Groups[1].Value;
            ProductID = match.Groups[2].Value;
            return;
        }
    }

    /// <summary>
    /// 尝试获取驱动程序版本
    /// </summary>
    /// <param name="version"></param>
    /// <returns></returns>
    public bool TryGetDriverVersion(out Version? version)
    {
        version = default;
        if (!string.IsNullOrEmpty(DriverKey))
        {
            var versionString = Registry.GetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{DriverKey}\", "DriverVersion", null) as string;
            // 可以给 Version.TryParse 传入 null 的值
            if (Version.TryParse(versionString!, out version))
            {
                return true;
            }
        }
        return false;
    }
}