// See https://aka.ms/new-console-template for more information

using JayhallchocojejalNabinojajarher.Devices;

// ProductID 来自 Intel 驱动
// 8086 为 Intel 的 VendorID
// 4C8A 为 i9-11900K 11900 11900T i7-11700K 11700 11700T i5-11600K 11600 11600T 11500 11500T 的核显 显示为 Intel(R) UHD Graphics 750
// 4C8B 为 i5-11400 11400T 的核显 显示为 Intel(R) UHD Graphics 730
// 9A78 为 i3-1125G4 1120G4 1115GRE 1115G4E 1115G4 1110G4 的核显 显示为 Intel(R) UHD Graphics
// 9A68 为 i5-11400H 11260H i3-11100HE 的核显 显示为 Intel(R) UHD Graphics
// 9A60 为 i9-11980HK 11950H 11900H i7-11850HE 11850H 11800H 11600H i5-11500HE 11500H 的核显 显示为 Intel(R) UHD Graphics
// 9A49 为 i7-11390H 11375H 11370H 1195G7 1185GRE 1185G7E 1185G7 1165G7 i5-11320H 11300H 1155G7 1145GRE 1145G7E 1145G7 1135G7 的核显 显示为 Intel(R) Iris(R) Xe Graphics
// 9A40 为 i7-1180G7 i5-1140G7 1130G7 的核显 显示为 Intel(R) Iris(R) Xe Graphics
// 以上应该为会受影响的核显 PID ，即所有英特尔11代 CPU 内的核显 / 第12代英特尔核芯显卡（英特尔为什么不把这两个代数的数字搞一样...），包括桌面端和移动端
// PS: 应该做不到装两个核显，只看 First ，不循环了
var device = DeviceDetector.GetPresentDeviceInfos(DeviceSetupClasses.GUID_DEVCLASS_DISPLAY, null)
    .FirstOrDefault(x => x.VendorID == "8086" &&
                         (x.DeviceID == "4C8A" || x.DeviceID == "4C8B" || x.DeviceID == "9A78" ||
                          x.DeviceID == "9A68" ||
                          x.DeviceID == "9A60" || x.DeviceID == "9A49" || x.DeviceID == "9A40")); // 耗时 20ms by lindexi
if (device != null)
{
    if (device.TryGetDriverVersion(out var version))
    {
        var fixedVersion = new Version(30, 0, 100, 9667);
        if (version >= fixedVersion)
        {
            // 问题已经修复
        }
        else
        {
            // 问题未修复
            // 此时可以切换到软渲染解决此问题
            // System.Windows.Media.RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            // 但必须知道的是软渲染会导致性能下降，且是依靠 CPU 来渲的，本身具备很大的限制性
        }
    }
}

Console.WriteLine("Hello, World!");
