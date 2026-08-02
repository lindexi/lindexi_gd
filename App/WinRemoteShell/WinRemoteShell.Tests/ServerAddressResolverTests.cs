using WinRemoteShell.Client;

namespace WinRemoteShell.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ServerAddressResolverTests
{
    [TestMethod]
    public void WhenCommandLineAddressIsSpecifiedThenItTakesPriority()
    {
        var original = Environment.GetEnvironmentVariable(ServerAddressResolver.EnvironmentVariableName);
        try
        {
            Environment.SetEnvironmentVariable(ServerAddressResolver.EnvironmentVariableName, "localhost:1000");

            var result = ServerAddressResolver.Resolve("localhost:2000");

            Assert.AreEqual(new Uri("http://localhost:2000"), result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ServerAddressResolver.EnvironmentVariableName, original);
        }
    }

    [TestMethod]
    public void WhenCommandLineAddressIsMissingThenEnvironmentAddressIsUsed()
    {
        var original = Environment.GetEnvironmentVariable(ServerAddressResolver.EnvironmentVariableName);
        try
        {
            Environment.SetEnvironmentVariable(ServerAddressResolver.EnvironmentVariableName, "localhost:3000");

            var result = ServerAddressResolver.Resolve(null);

            Assert.AreEqual(new Uri("http://localhost:3000"), result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ServerAddressResolver.EnvironmentVariableName, original);
        }
    }
}