namespace WinRemoteShell.Client;

public static class ServerAddressResolver
{
    public const string EnvironmentVariableName = "WINRS_SERVER";

    /// <summary>
    /// Resolves the server address from the command line or environment.
    /// </summary>
    public static Uri Resolve(string? server)
    {
        var address = string.IsNullOrWhiteSpace(server)
            ? Environment.GetEnvironmentVariable(EnvironmentVariableName)
            : server;

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new InvalidOperationException($"Specify --server or set {EnvironmentVariableName}.");
        }

        if (!address.Contains("://", StringComparison.Ordinal))
        {
            address = $"http://{address}";
        }

        return new Uri(address, UriKind.Absolute);
    }
}
