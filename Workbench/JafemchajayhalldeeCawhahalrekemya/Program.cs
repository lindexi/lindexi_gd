// See https://aka.ms/new-console-template for more information

using System.Text.Json;

var dictionary = new Dictionary<string, string>();
dictionary["aaa"] = "bbb";
dictionary["ccc"] = "ddd";
dictionary["eee"] = "fff";

var foo = new Foo()
{
    Lang = dictionary
};

var json = JsonSerializer.Serialize(foo, new JsonSerializerOptions()
{
    WriteIndented = true
});

var foo2 = JsonSerializer.Deserialize<Foo>(json);

Console.WriteLine(json);

class Foo
{
    public Dictionary<string, string>? Lang { get; set; }
}