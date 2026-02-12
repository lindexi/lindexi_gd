using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateSlimBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

var app = builder.Build();

app.MapPost("/", (FooRequest request) =>
{
    return "OK";
});

app.Run();

public class FooRequest
{
    public int Id { get; init; } 
    public required string Title { get; init; } 
}
