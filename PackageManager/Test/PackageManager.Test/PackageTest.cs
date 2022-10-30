using System.Net;
using System.Net.Http.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;
using PackageManager.Server.Controllers;

namespace PackageManager.Test;

[TestClass]
public class PackageTest
{
    [ContractTestCase]
    public void Put()
    {
        "调用 Put 推送包时，在 Header 带上错误的 Token 将会返回 NotFound 错误".Test(async () =>
        {
            var httpClient = TestFramework.GetTestClient();

            var putPackageRequest = new PutPackageRequest();
            var jsonContent = JsonContent.Create(putPackageRequest);
            httpClient.DefaultRequestHeaders.Add("Token","Error");
            var result = await httpClient.PutAsync("/Package", jsonContent);

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
        });

        "调用 Put 推送包时，没有在 Header 带上 Token 将会返回 NotFound 错误".Test(async () =>
        {
            var httpClient = TestFramework.GetTestClient();

            var putPackageRequest = new PutPackageRequest();
            var result = await httpClient.PutAsync("/Package", JsonContent.Create(putPackageRequest));

            Assert.AreEqual(HttpStatusCode.NotFound,result.StatusCode);
        });
    }

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