using WatchDog.Service.Controllers;

namespace WatchDog.Service;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        builder.AddWatchDog();
        builder.WebHost.UseUrls("http://0.0.0.0:57725");

        var app = builder.Build();

        // Configure the HTTP request pipeline.

        app.UseAuthorization();
        app.UseWatchDog();


        app.MapControllers();

        app.Run();
    }
}
