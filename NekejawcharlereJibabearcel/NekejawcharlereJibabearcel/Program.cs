using System.Buffers;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://*:12367");
builder.WebHost.UseKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1024_0000_0000_0000_000;
});
var app = builder.Build();

app.MapPost("/Upload", async context =>
{
    var length = 1024 * 1024 * 100;
    var buffer = ArrayPool<byte>.Shared.Rent(length);

    int count;
    while ((count = await context.Request.Body.ReadAsync(buffer, 0, length)) > 0)
    {
        await Task.Delay(1000);
    }

    ArrayPool<byte>.Shared.Return(buffer);

    context.Response.StatusCode = StatusCodes.Status200OK;
    await context.Response.WriteAsync("Hello World!");
});

app.MapPost("/UploadTimeout", async context =>
{
    var length = 1024 * 1024 * 100;
    var buffer = ArrayPool<byte>.Shared.Rent(length);

    int count;
    int n = 0;
    while ((count = await context.Request.Body.ReadAsync(buffer, 0, length)) > 0)
    {
        await Task.Delay(1000);
        n++;
        if (n == 10)
        {
            await Task.Delay(TimeSpan.FromHours(10));
        }
    }

    ArrayPool<byte>.Shared.Return(buffer);

    context.Response.StatusCode = StatusCodes.Status200OK;
    await context.Response.WriteAsync("Hello World!");
});

app.Run();
