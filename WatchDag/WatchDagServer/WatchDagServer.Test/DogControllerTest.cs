using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;
using WatchDagServer.Model;

namespace WatchDagServer.Test
{
    [TestClass]
    public class DogControllerTest
    {
        [ContractTestCase]
        public void FeedDog()
        {
            "尝试访问喂狗方法，可以访问到方法".Test(async () =>
            {
                var testClient = TestHostBuild.GetTestClient();
                var response = await testClient.GetAsync("Dog/FeedDog");
                Assert.AreEqual(true, response.StatusCode == HttpStatusCode.OK);
            });
        }

        [ContractTestCase]
        public void RegisterWatch()
        {
            "在没有超过规定时间内喂狗，不会被咬".Test(async () =>
            {
                var testClient = TestHostBuild.GetTestClient();

                var registerRequest = new RegisterRequest()
                {
                    Token = Guid.NewGuid().ToString("N"),
                    DelaySecond = 2,
                    MaxDelayCount = 5,
                    NotifyEmailList = new[] { "lindexi@doubi.com" }
                };

                var response = await testClient.PostAsJsonAsync("Dog/RegisterWatch", registerRequest);

                Thread.Sleep(3000);

                await testClient.PostAsJsonAsync("Dog/RegisterWatch", registerRequest);

                Thread.Sleep(1000000);
            });

            "在自己规定时间内喂狗，不会被咬".Test(async () =>
            {
                var testClient = TestHostBuild.GetTestClient();

                var registerRequest = new RegisterRequest()
                {
                    Token = Guid.NewGuid().ToString("N"),
                    DelaySecond = 2,
                    MaxDelayCount = 5,
                    NotifyEmailList = new[] { "lindexi@doubi.com" }
                };

                var response = await testClient.PostAsJsonAsync("Dog/RegisterWatch", registerRequest);

                Thread.Sleep(1000);

                await testClient.PostAsJsonAsync("Dog/RegisterWatch", registerRequest);
            });

            "测试传入参数".Test(async () =>
            {
                var testClient = TestHostBuild.GetTestClient();

                var registerRequest = new RegisterRequest()
                {
                    Token = Guid.NewGuid().ToString("N"),
                    DelaySecond = 2,
                    MaxDelayCount = 5,
                    NotifyEmailList = new []{ "lindexi@doubi.com" }
                };

                var response = await testClient.PostAsJsonAsync("Dog/RegisterWatch", registerRequest);

                Thread.Sleep(1000000);
            });

            "测试注册".Test(async () =>
            {
                var testClient = TestHostBuild.GetTestClient();
                var response = await testClient.GetAsync("Dog/RegisterWatch?token=as");

              });
        }
    }
}
