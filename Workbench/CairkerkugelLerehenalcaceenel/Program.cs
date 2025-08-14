using CheejairkafeCayfelnoyikilur;

using Microsoft.AspNetCore.Diagnostics;

using System.Diagnostics;

namespace CairkerkugelLerehenalcaceenel
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("asdfasdasdasd");

            Debugger.Break();

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            builder.Services.AddExceptionHandler<CustomExceptionHandler>();

            var app = builder.Build();
            app.Map("/", () => TestService.Greet);
            app.MapGet("/weatherforecast", () => "Hello from Foo");


            //app.UseExceptionHandler(new ExceptionHandlerOptions()
            //{
            //    StatusCodeSelector = exception =>
            //    {
            //        Debugger.Break();
            //        Debugger.Log(0,"Exception", exception.ToString());

            //        return 501;
            //    },
            //    ExceptionHandler = async context =>
            //    {
            //        Debugger.Break();

            //        await Task.CompletedTask;
            //    }
            //});
            app.UseExceptionHandler(exceptionHandlerApp =>
            {
                Debugger.Break();
                exceptionHandlerApp.Run( context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                    Debugger.Break();

                    return Task.CompletedTask;
                });
            });

            // Configure the HTTP request pipeline.

            //app.UseAuthorization();

            app.Urls.Add("http://127.0.0.1:5123");

            //app.MapControllers();

            app.Run();
        }
    }

    public class CustomExceptionHandler : IExceptionHandler
    {
        public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            Debugger.Log(1,"asdf", exception.ToString());
            Debugger.Launch();
            Debugger.Break();
            return new ValueTask<bool>(false);
        }
    }
}
