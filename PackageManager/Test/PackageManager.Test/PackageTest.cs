using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;

namespace PackageManager.Test;

[TestClass]
public class PackageTest
{
    [ContractTestCase]
    public void Get()
    {
        "测试默认的 Get 方法，可以有返回值，证明有联通".Test(async () =>
        {
            var httpClient = TestFramework.GetTestClient();

            var result = await  httpClient.GetStringAsync("/Package");

            Assert.IsNotNull(result);
        });
    }
}