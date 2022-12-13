namespace KarhiheaqayemdalweFaiwaljuwe;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();

        var app = builder.Build();

        // Configure the HTTP request pipeline.

        app.Urls.Add("http://127.0.0.1:5121");
        app.Urls.Add("https://127.0.0.1:5122");

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}
