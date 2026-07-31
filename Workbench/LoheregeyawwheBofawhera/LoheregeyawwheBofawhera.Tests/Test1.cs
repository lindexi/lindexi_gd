using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace LoheregeyawwheBofawhera.Tests;

[TestClass]
public sealed class NetworkEnvironmentTests
{
    [TestMethod]
    public void WhenOnlyIpv6IsAvailableThenEnvironmentIsIpv6Only()
    {
        NetworkAdapterSnapshot[] adapters =
        [
            new("Ethernet", true, false, true, false),
        ];

        NetworkEnvironmentKind result = NetworkEnvironmentDetector.Classify(adapters);

        Assert.AreEqual(NetworkEnvironmentKind.Ipv6Only, result);
    }

    [TestMethod]
    public void WhenIpv4AndIpv6AreAvailableWithoutIpv4GatewayThenEnvironmentIsDualStackWithoutIpv4Gateway()
    {
        NetworkAdapterSnapshot[] adapters =
        [
            new("Ethernet", true, true, true, false),
        ];

        NetworkEnvironmentKind result = NetworkEnvironmentDetector.Classify(adapters);

        Assert.AreEqual(NetworkEnvironmentKind.DualStackWithoutIpv4Gateway, result);
    }

    [TestMethod]
    public void WhenIpv4AddressAndIpv6AddressAreOnDifferentAdaptersWithoutIpv4GatewayThenEnvironmentIsDualStackWithoutIpv4Gateway()
    {
        NetworkAdapterSnapshot[] adapters =
        [
            new("IPv4 adapter", true, true, false, false),
            new("IPv6 adapter", true, false, true, false),
        ];

        NetworkEnvironmentKind result = NetworkEnvironmentDetector.Classify(adapters);

        Assert.AreEqual(NetworkEnvironmentKind.DualStackWithoutIpv4Gateway, result);
    }

    [TestMethod]
    public void WhenIpv4AndIpv6AreAvailableWithIpv4GatewayThenEnvironmentIsDualStackWithIpv4Gateway()
    {
        NetworkAdapterSnapshot[] adapters =
        [
            new("Ethernet", true, true, true, true),
        ];

        NetworkEnvironmentKind result = NetworkEnvironmentDetector.Classify(adapters);

        Assert.AreEqual(NetworkEnvironmentKind.DualStackWithIpv4Gateway, result);
    }

    [TestMethod]
    public void WhenOnlyIpv4IsAvailableWithoutGatewayThenEnvironmentIsIpv4OnlyWithoutGateway()
    {
        NetworkAdapterSnapshot[] adapters =
        [
            new("Ethernet", true, true, false, false),
        ];

        NetworkEnvironmentKind result = NetworkEnvironmentDetector.Classify(adapters);

        Assert.AreEqual(NetworkEnvironmentKind.Ipv4OnlyWithoutGateway, result);
    }

    [TestMethod]
    public void WhenOnlyDownAdaptersHaveAddressesThenEnvironmentHasNoUsableIpNetwork()
    {
        NetworkAdapterSnapshot[] adapters =
        [
            new("Disconnected Ethernet", false, true, true, true),
        ];

        NetworkEnvironmentKind result = NetworkEnvironmentDetector.Classify(adapters);

        Assert.AreEqual(NetworkEnvironmentKind.NoUsableIpNetwork, result);
    }

    [TestMethod]
    public void InspectCurrentMachineNetworkEnvironment()
    {
        NetworkEnvironmentSnapshot snapshot = NetworkEnvironmentDetector.InspectCurrentMachine();

        Console.WriteLine($"Current classification: {snapshot.Kind}");
        foreach (NetworkAdapterSnapshot adapter in snapshot.Adapters)
        {
            Console.WriteLine(
                $"{adapter.Name}: Up={adapter.IsUp}, IPv4={adapter.HasIpv4}, " +
                $"IPv6={adapter.HasIpv6}, IPv4Gateway={adapter.HasIpv4Gateway}");
        }

        Assert.AreEqual(snapshot.Kind, NetworkEnvironmentDetector.Classify(snapshot.Adapters));
    }
}

internal static class NetworkEnvironmentDetector
{
    public static NetworkEnvironmentSnapshot InspectCurrentMachine()
    {
        NetworkAdapterSnapshot[] adapters = NetworkInterface.GetAllNetworkInterfaces()
            .Where(networkInterface => networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Select(CreateSnapshot)
            .ToArray();

        return new NetworkEnvironmentSnapshot(Classify(adapters), adapters);
    }

    public static NetworkEnvironmentKind Classify(IEnumerable<NetworkAdapterSnapshot> adapters)
    {
        ArgumentNullException.ThrowIfNull(adapters);

        NetworkAdapterSnapshot[] activeAdapters = adapters.Where(adapter => adapter.IsUp).ToArray();
        bool hasIpv4 = activeAdapters.Any(adapter => adapter.HasIpv4);
        bool hasIpv6 = activeAdapters.Any(adapter => adapter.HasIpv6);
        bool hasIpv4Gateway = activeAdapters.Any(adapter => adapter.HasIpv4Gateway);

        return (hasIpv4, hasIpv6, hasIpv4Gateway) switch
        {
            (false, true, _) => NetworkEnvironmentKind.Ipv6Only,
            (true, true, false) => NetworkEnvironmentKind.DualStackWithoutIpv4Gateway,
            (true, true, true) => NetworkEnvironmentKind.DualStackWithIpv4Gateway,
            (true, false, false) => NetworkEnvironmentKind.Ipv4OnlyWithoutGateway,
            (true, false, true) => NetworkEnvironmentKind.Ipv4OnlyWithGateway,
            _ => NetworkEnvironmentKind.NoUsableIpNetwork,
        };
    }

    private static NetworkAdapterSnapshot CreateSnapshot(NetworkInterface networkInterface)
    {
        IPInterfaceProperties properties = networkInterface.GetIPProperties();
        bool isUp = networkInterface.OperationalStatus == OperationalStatus.Up;
        bool hasIpv4 = properties.UnicastAddresses.Any(address => IsUsableIpv4(address.Address));
        bool hasIpv6 = properties.UnicastAddresses.Any(address => IsUsableIpv6(address.Address));
        bool hasIpv4Gateway = properties.GatewayAddresses.Any(gateway => IsUsableIpv4Gateway(gateway.Address));

        return new NetworkAdapterSnapshot(networkInterface.Name, isUp, hasIpv4, hasIpv6, hasIpv4Gateway);
    }

    private static bool IsUsableIpv4(IPAddress address)
    {
        return address.AddressFamily == AddressFamily.InterNetwork
            && !IPAddress.Any.Equals(address)
            && !IPAddress.Loopback.Equals(address);
    }

    private static bool IsUsableIpv6(IPAddress address)
    {
        return address.AddressFamily == AddressFamily.InterNetworkV6
            && !IPAddress.IPv6Any.Equals(address)
            && !IPAddress.IPv6Loopback.Equals(address);
    }

    private static bool IsUsableIpv4Gateway(IPAddress address)
    {
        return address.AddressFamily == AddressFamily.InterNetwork
            && !IPAddress.Any.Equals(address)
            && !IPAddress.Loopback.Equals(address);
    }
}

internal sealed record NetworkEnvironmentSnapshot(
    NetworkEnvironmentKind Kind,
    IReadOnlyList<NetworkAdapterSnapshot> Adapters);

internal sealed record NetworkAdapterSnapshot(
    string Name,
    bool IsUp,
    bool HasIpv4,
    bool HasIpv6,
    bool HasIpv4Gateway);

internal enum NetworkEnvironmentKind
{
    NoUsableIpNetwork,
    Ipv6Only,
    DualStackWithoutIpv4Gateway,
    DualStackWithIpv4Gateway,
    Ipv4OnlyWithoutGateway,
    Ipv4OnlyWithGateway,
}
