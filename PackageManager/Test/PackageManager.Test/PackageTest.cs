using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MSTest.Extensions.Contracts;

using PackageManager.Server.Controllers;
using PackageManager.Server.Model;

namespace PackageManager.Test;

[TestClass]
public class PackageTest
{
    [ContractTestCase]
    public void Put()
    {
        "推送一个包，可以推送成功".Test(async () =>
        {
            var httpClient = TestFramework.GetTestClient();

            var putPackageRequest = GetTestPutPackageRequest();
            var jsonContent = JsonContent.Create(putPackageRequest);
            httpClient.DefaultRequestHeaders.Add("Token", TokenConfiguration.Token);

            var result = await httpClient.PutAsync("/Package", jsonContent);

            // 判断推送成功需要判断是否加入
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

            var packageResponse = await httpClient.GetFromJsonAsync<GetPackageResponse>($"/Package?PackageId={putPackageRequest.PackageInfo.PackageId}");

            Assert.AreEqual(putPackageRequest.PackageInfo.PackageId,packageResponse.PackageInfo.PackageId);

            var list = await httpClient.GetFromJsonAsync<List<PackageInfo>>("/Package/GetPackageListInMainPage");
            Assert.IsNotNull(list);
            Assert.AreEqual(true, list.Any(t => t.PackageId == putPackageRequest.PackageInfo.PackageId));
        });

        "调用 Put 推送包时，在 Header 带上错误的 Token 将会返回 NotFound 错误".Test(async () =>
        {
            var httpClient = TestFramework.GetTestClient();

            var putPackageRequest = GetTestPutPackageRequest();
            var jsonContent = JsonContent.Create(putPackageRequest);
            httpClient.DefaultRequestHeaders.Add("Token", "Error");
            var result = await httpClient.PutAsync("/Package", jsonContent);

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
        });

        "调用 Put 推送包时，没有在 Header 带上 Token 将会返回 NotFound 错误".Test(async () =>
        {
            var httpClient = TestFramework.GetTestClient();

            var putPackageRequest = GetTestPutPackageRequest();
            var result = await httpClient.PutAsync("/Package", JsonContent.Create(putPackageRequest));

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
        });

        PutPackageRequest GetTestPutPackageRequest() => new PutPackageRequest(new PackageInfo()
        {
            PackageId = "Test",
            Author = "lindexi",
            Name = "Test",
            CanShow = true,
        });
    }

    [ContractTestCase]
    public void Get()
    {
        "测试默认的 Get 方法，可以有返回值，证明有联通".Test(async () =>
        {
            var httpClient = TestFramework.GetTestClient();

            var result = await httpClient.GetStringAsync("/Package");

            Assert.IsNotNull(result);
        });
    }
}