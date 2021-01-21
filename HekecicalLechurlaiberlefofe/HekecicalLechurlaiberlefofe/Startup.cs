using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace HekecicalLechurlaiberlefofe
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers(options =>
                    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            //// 这里是不会进来的
            //app.UseExceptionHandler(new ExceptionHandlerOptions()
            //{
            //    ExceptionHandler = context =>
            //    {
            //        return Task.CompletedTask;
            //    }
            //});

            app.UseExceptionHandler(builder =>
            {
                // 这是会进来的
            });

            app.Use((context, func) =>
            {
                return func();
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
