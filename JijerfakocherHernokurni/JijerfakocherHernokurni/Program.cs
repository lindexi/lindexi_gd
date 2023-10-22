
using System.Net.Sockets;
using System.Net;
using JijerfakocherHernokurni.Controllers;

var port = GetAvailablePort(IPAddress.Loopback);
var url = $"http://127.0.0.1:{port}";

Task.Run(async () =>
{
    var httpClient = new HttpClient();
    httpClient.BaseAddress = new Uri(url);
    var request = new F1Request("Name");
    var response = await httpClient.PostAsJsonAsync("/WeatherForecast", request);
});

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options =>
    // 警惕可空类型开启之后模型校验失败
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

builder.WebHost.UseUrls(url);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseAuthorization();

app.MapControllers();

app.Run();

static int GetAvailablePort(IPAddress ip)
{
    using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
    socket.Bind(new IPEndPoint(ip, 0));
    socket.Listen(1);
    var ipEndPoint = (IPEndPoint) socket.LocalEndPoint!;
    var port = ipEndPoint.Port;
    return port;
}