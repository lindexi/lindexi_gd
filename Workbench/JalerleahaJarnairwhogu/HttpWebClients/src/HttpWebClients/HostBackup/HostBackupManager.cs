using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpWebClients.HostBackup;

internal class HostBackupManager
{
    // 业务层的域名备份是有用的
    // 比如有域名劫持的情况，将一个域名劫持了。此时 Socket 能够连接成功，但是后续的证书等步骤失败。此时换一个域名也许就能活了
    // 或者是 DNS 解析失败的情况，主域名就是解析失败了，但此时备用的域名可以正常解析
    // 对于 CDN 厂商的域名备份的情况。假定能够给注入两个厂商，如 A 和 B 两个
    // 如果 A 的 IP 连接失败的情况，不要立刻去遍历 A 的其他 IP 地址，而是先跳到 B 厂商去连接
    // 如果 B 厂商的 IP 连接失败了，再回到 A 厂商去连接其他 IP 地址
    // 这样的情况是解决这样的问题： A 厂商的 IP 段都可能被封锁，连接都是失败了。如果遍历整个 A 厂商的 IP 段，当失败时，用户会等太久时间。此时先跳到 B 厂商去连接。同时挂掉两个 CDN 厂商的情况是不多的
}
