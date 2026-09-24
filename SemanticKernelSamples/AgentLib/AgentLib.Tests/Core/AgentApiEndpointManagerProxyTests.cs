using System.Net;

using AgentLib.Core;

namespace AgentLib.Tests.Core;

[TestClass]
public sealed class AgentApiEndpointManagerProxyTests
{
    [TestMethod]
    public void WebProxy_WhenChanged_DoesNotReplaceHttpClient()
    {
        var manager = new AgentApiEndpointManager();
        HttpClient? httpClient = manager.HttpClient;

        manager.WebProxy = new WebProxy("http://127.0.0.1:7890");

        Assert.AreSame(httpClient, manager.HttpClient);
    }

    [TestMethod]
    public void DynamicWebProxy_WhenProxyChanges_UsesCurrentProxy()
    {
        var dynamicProxy = new DynamicWebProxy();
        var destination = new Uri("https://example.com");
        var proxyAddress = new Uri("http://127.0.0.1:7890");

        dynamicProxy.Proxy = new WebProxy(proxyAddress);

        Assert.AreEqual(proxyAddress, dynamicProxy.GetProxy(destination));
    }

    [TestMethod]
    public void DynamicWebProxy_WhenProxyIsNull_BypassesRequest()
    {
        var dynamicProxy = new DynamicWebProxy
        {
            Proxy = new WebProxy("http://127.0.0.1:7890"),
        };
        var destination = new Uri("https://example.com");

        dynamicProxy.Proxy = null;

        Assert.IsTrue(dynamicProxy.IsBypassed(destination));
    }
}
