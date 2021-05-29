using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;

namespace HihukekelralCayagaynofo
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            Console.WriteLine($"01 ConfigureServices");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
            });
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            Console.WriteLine($"03 ConfigureContainer");
            builder.RegisterModule(new FooModule());
        }
    }

    class FooModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            Console.WriteLine($"06 FooModule");

            builder.RegisterType<Foo>().As<IFoo>();
        }
    }

    public class Foo : IFoo
    {

    }

   public interface IFoo
    {
        
    }
}
