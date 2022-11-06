using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;
using Newtonsoft.Json;
using PackageManager.Server.Controllers;
using PackageManager.Server.Model;
using PackageManager.Server.Utils;

namespace PackageManager.Test;

[TestClass]
public class PackageTest
{
    [ContractTestCase]
    public void Put()
    {
        "测试推送相同的Id的不同版本的包，最新记录的只有最后一次推送".Test(async () =>
        {
            var httpClient = TestFramework.GetTestClient();
            httpClient.DefaultRequestHeaders.Add("Token", TokenConfiguration.Token);

            var packageId = Guid.NewGuid().ToString();

            for (int i = 0; i < 100; i++)
            {
                // 推送相同的Id的不同版本的包
                var putPackageRequest = GetTestPutPackageRequest();
                putPackageRequest.PackageInfo.PackageId = packageId;
                putPackageRequest.PackageInfo.Version=new Version($"1.0.{i}").VersionToLong();

                var jsonContent = JsonContent.Create(putPackageRequest);

                var result = await httpClient.PutAsync("/Package", jsonContent);
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            }

            // 最新记录的只有最后一次推送
            var response = await httpClient.GetAsync($"/Package?ClientVersion=1.0&PackageId={packageId}");
            var text = await response.Content.ReadAsStringAsync();
            var (message, packageInfo) = JsonConvert.DeserializeObject<GetPackageResponse>(text);
            Assert.IsNotNull(packageInfo);
            Assert.AreEqual(packageId, packageInfo.PackageId);
            Assert.AreEqual($"1.0.99", packageInfo.Version);
        });

        "测试推送一个包，这个包的描述字符串十分长".Test(async () =>
        {
            var httpClient = TestFramework.GetTestClient();

            var putPackageRequest = GetTestPutPackageRequest();
            var length = 100_0000;
            var stringBuilder = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                stringBuilder.Append('A');
            }

            putPackageRequest.PackageInfo.PackageId = "TestA";
            putPackageRequest.PackageInfo.Description = stringBuilder.ToString();
            var jsonContent = JsonContent.Create(putPackageRequest);
            httpClient.DefaultRequestHeaders.Add("Token", TokenConfiguration.Token);

            var result = await httpClient.PutAsync("/Package", jsonContent);

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        });

        "推送一个包，可以推送成功".Test(async () =>
        {
            var httpClient = TestFramework.GetTestClient();

            var putPackageRequest = GetTestPutPackageRequest();
            var jsonContent = JsonContent.Create(putPackageRequest);
            httpClient.DefaultRequestHeaders.Add("Token", TokenConfiguration.Token);

            var result = await httpClient.PutAsync("/Package", jsonContent);

            // 判断推送成功需要判断是否加入
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

            var packageResponse =
                await httpClient.GetFromJsonAsync<GetPackageResponse>(
                    $"/Package?PackageId={putPackageRequest.PackageInfo.PackageId}");

            Assert.AreEqual(putPackageRequest.PackageInfo.PackageId, packageResponse.PackageInfo.PackageId);

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

        
    }

    [ContractTestCase]
    public void Get()
    {
        "传入客户端版本比能支持的版本号小，返回找不到可用的资源".Test(async () =>
        {
            var httpClient = TestFramework.GetTestClient();

            var packageId = Guid.NewGuid().ToString();

            var testPutPackageRequest = GetTestPutPackageRequest();
            testPutPackageRequest.PackageInfo.PackageId = packageId;
            testPutPackageRequest.PackageInfo.SupportMinClientVersion=new Version("1.0.100").VersionToLong();

            // 先推送一个版本过去
            await PutPackage(httpClient, testPutPackageRequest);
            // 然后申请一个客户端版本比能支持的更小的
            var clientVersion = "1.0.99";
            var response = await httpClient.GetFromJsonAsync<GetPackageResponse>($"/Package?ClientVersion={clientVersion}&PackageId={packageId}");

            // 返回找不到可用的资源
            Assert.AreEqual(true, response?.IsNotFound);
        });

        "测试默认的 Get 方法，可以有返回值，证明有联通".Test(async () =>
        {
            var httpClient = TestFramework.GetTestClient();

            var result = await httpClient.GetStringAsync("/Package");

            Assert.IsNotNull(result);
        });
    }

    private async Task<HttpResponseMessage> PutPackage(HttpClient httpClient, PutPackageRequest putPackageRequest)
    {
        if (httpClient.DefaultRequestHeaders.TryGetValues("Token", out _))
        {

        }
        else
        {
            httpClient.DefaultRequestHeaders.Add("Token", TokenConfiguration.Token);
        }

        var jsonContent = JsonContent.Create(putPackageRequest);

        var result = await httpClient.PutAsync("/Package", jsonContent);
        return result;
    }

    private PutPackageRequest GetTestPutPackageRequest() => new PutPackageRequest(new PackageInfo()
    {
        PackageId = "Test",
        Author = "lindexi",
        Name = "Test",
        CanShow = true,
    });
}