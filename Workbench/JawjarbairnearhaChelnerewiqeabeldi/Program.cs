
using System.Collections.ObjectModel;
using System.Net;
using Microsoft.ML.OnnxRuntimeGenAI;

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Yarp.ReverseProxy.Configuration;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy().LoadFromMemory([new RouteConfig()
{
    RouteId = "route1",
    ClusterId = "cluster1",
    Match = new RouteMatch()
    {
        Path = "{**catch-all}"
    }
}], [new ClusterConfig()
{
    ClusterId = "cluster1",
    Destinations = new ReadOnlyDictionary<string, DestinationConfig>(new Dictionary<string, DestinationConfig>()
    {
        {"cluster1/destination1", new DestinationConfig()
        {
            Address = "http://172.28.254.77/"
        }}
    })
}]);

builder.WebHost.UseUrls("http://0.0.0.0:5067");
builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSimpleConsole());
builder.Services.AddHttpClient();
// Add services to the container.

var app = builder.Build();

app.MapReverseProxy();

int current = 0;
int total = 0;

app.MapGet("/Status", () => $"Current={current};Total={total}");


app.Run();
