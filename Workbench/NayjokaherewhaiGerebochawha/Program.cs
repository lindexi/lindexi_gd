// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using System.Text.Json.Serialization;

var json = """
{
    "aaa": "value1",
    "Aaa": "value2"
}
""";

var foo = JsonSerializer.Deserialize<Foo>(json, new JsonSerializerOptions()
{
});

Console.WriteLine("Hello, World!");


class Foo
{
    [JsonPropertyName("aaa")]
    public required string P1 { get; init; }

    [JsonPropertyName("Aaa")]
    public required string P2 { get; init; }
}