using System;
using System.Net.Http;
using System.Threading.Channels;
using System.Threading.Tasks;
using Autofac;
using Autofac.Builder;
using Autofac.Extensions.DependencyInjection;
using HihukekelralCayagaynofo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;

namespace KayjurhaynoFereharniwi
{
    [TestClass]
    public class FooTest
    {
        [ContractTestCase]
        public void Test()
        {
            "依赖注入的时机，可以在完成收集之后，覆盖原有的类型".Test(() =>
            {
                var hostBuilder = Host.CreateDefaultBuilder()
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseStartup<Startup>();
                        webBuilder.UseTestServer(); //关键是多了这一行建立TestServer
                    })
                    .ConfigureServices(collection => Console.WriteLine($"02 ConfigureServices Delegate"))
                    // 使用 auto fac 代替默认的 IOC 容器 
                    .UseServiceProviderFactory(new FakeAutofacServiceProviderFactory(configurationActionOnAfter:
                        builder =>
                        {
                            Console.WriteLine($"05 ConfigurationActionOnAfter");
                            builder.RegisterModule<TestModule>();
                        }));

                var host = hostBuilder.Build();

                var foo = host.Services.GetService<IFoo>();
                Assert.IsInstanceOfType(foo, typeof(TestFoo));
            });
        }
    }

    class TestModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            Console.WriteLine($"07 TestModule");

            builder.RegisterType<TestFoo>().As<IFoo>();
        }
    }

    class TestFoo : IFoo
    {
    }

    class FakeAutofacServiceProviderFactory : IServiceProviderFactory<ContainerBuilder>
    {
        public FakeAutofacServiceProviderFactory(
            ContainerBuildOptions containerBuildOptions = ContainerBuildOptions.None,
            Action<ContainerBuilder>? configurationActionOnBefore = null,
            Action<ContainerBuilder>? configurationActionOnAfter = null)
        {
            _configurationActionOnAfter = configurationActionOnAfter;
            AutofacServiceProviderFactory =
                new AutofacServiceProviderFactory(containerBuildOptions, configurationActionOnBefore);
        }

        private AutofacServiceProviderFactory AutofacServiceProviderFactory { get; }
        private readonly Action<ContainerBuilder>? _configurationActionOnAfter;

        public ContainerBuilder CreateBuilder(IServiceCollection services)
        {
            return AutofacServiceProviderFactory.CreateBuilder(services);
        }

        public IServiceProvider CreateServiceProvider(ContainerBuilder containerBuilder)
        {
            Console.WriteLine($"04 FakeAutofacServiceProviderFactory");
            _configurationActionOnAfter?.Invoke(containerBuilder);
            return AutofacServiceProviderFactory.CreateServiceProvider(containerBuilder);
        }
    }
}