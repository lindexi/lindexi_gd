using PackageManager.Server.Context;

namespace PackageManager.Server;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var file = Path.Combine("PackageManger.db");
        file = Path.GetFullPath(file);
        var connect = $"Filename={file}";

        services.AddControllers();
        services.AddSqlite<PackageManagerContext>(connect);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(builder => builder.MapControllers());
    }
}