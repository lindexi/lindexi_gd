using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

Console.WriteLine($"共发现 {networkInterfaces.Length} 个网络接口。{Environment.NewLine}");

foreach (NetworkInterface networkInterface in networkInterfaces)
{
    Console.WriteLine(new string('=', 80));
    Console.WriteLine($"名称: {networkInterface.Name}");
    Console.WriteLine($"描述: {networkInterface.Description}");
    Console.WriteLine($"类型: {networkInterface.NetworkInterfaceType}");
    Console.WriteLine($"运行状态: {networkInterface.OperationalStatus}");
    Console.WriteLine($"物理地址: {FormatPhysicalAddress(networkInterface.GetPhysicalAddress())}");
    Console.WriteLine($"速度: {FormatSpeed(networkInterface.Speed)}");
    Console.WriteLine($"支持 IPv4: {networkInterface.Supports(NetworkInterfaceComponent.IPv4)}");
    Console.WriteLine($"支持 IPv6: {networkInterface.Supports(NetworkInterfaceComponent.IPv6)}");

    try
    {
        IPInterfaceProperties properties = networkInterface.GetIPProperties();

        PrintUnicastAddresses(properties.UnicastAddresses);
        PrintAddresses("DNS 服务器", properties.DnsAddresses);
        PrintGateways(properties.GatewayAddresses);

        Console.WriteLine($"启用 DNS: {properties.IsDnsEnabled}");
        Console.WriteLine($"动态 DNS: {properties.IsDynamicDnsEnabled}");
        Console.WriteLine($"DNS 后缀: {FormatOptionalValue(properties.DnsSuffix)}");
    }
    catch (NetworkInformationException exception)
    {
        Console.WriteLine($"无法读取该网络接口的 IP 配置: {exception.Message}");
    }

    Console.WriteLine();
}

static void PrintUnicastAddresses(UnicastIPAddressInformationCollection addresses)
{
    Console.WriteLine("单播 IP 地址:");

    if (addresses.Count == 0)
    {
        Console.WriteLine("  （无）");
        return;
    }

    foreach (UnicastIPAddressInformation addressInformation in addresses)
    {
        IPAddress address = addressInformation.Address;
        string protocol = GetProtocolName(address);
        string prefix = GetPrefixDescription(addressInformation);

        Console.WriteLine($"  [{protocol}] {address}{prefix}");
    }
}

static void PrintAddresses(string title, IPAddressCollection addresses)
{
    Console.WriteLine($"{title}:");

    if (addresses.Count == 0)
    {
        Console.WriteLine("  （无）");
        return;
    }

    foreach (IPAddress address in addresses)
    {
        Console.WriteLine($"  [{GetProtocolName(address)}] {address}");
    }
}

static void PrintGateways(GatewayIPAddressInformationCollection gateways)
{
    Console.WriteLine("网关:");

    if (gateways.Count == 0)
    {
        Console.WriteLine("  （无）");
        return;
    }

    foreach (GatewayIPAddressInformation gateway in gateways)
    {
        Console.WriteLine($"  [{GetProtocolName(gateway.Address)}] {gateway.Address}");
    }
}

static string GetProtocolName(IPAddress address)
{
    return address.AddressFamily switch
    {
        AddressFamily.InterNetwork => "IPv4",
        AddressFamily.InterNetworkV6 => "IPv6",
        _ => address.AddressFamily.ToString(),
    };
}

static string GetPrefixDescription(UnicastIPAddressInformation addressInformation)
{
    try
    {
        return $"/{addressInformation.PrefixLength}";
    }
    catch (PlatformNotSupportedException)
    {
        return string.Empty;
    }
}

static string FormatPhysicalAddress(PhysicalAddress address)
{
    byte[] bytes = address.GetAddressBytes();
    return bytes.Length == 0 ? "（无）" : string.Join('-', bytes.Select(value => value.ToString("X2")));
}

static string FormatSpeed(long speed)
{
    return speed > 0 ? $"{speed:N0} bit/s" : "未知";
}

static string FormatOptionalValue(string? value)
{
    return string.IsNullOrWhiteSpace(value) ? "（无）" : value;
}
