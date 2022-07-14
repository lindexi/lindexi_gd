// See https://aka.ms/new-console-template for more information

using System.Runtime.Serialization.Json;
using System.Text.Json;

var file = @"D:\lindexi\app-package.json";

var json = File.ReadAllText(file);

using var jsonDocument = JsonDocument.Parse(json);
var jsonElement = jsonDocument.RootElement.GetProperty("distribution_tag");
var value = jsonElement.GetString();
Console.WriteLine("Hello, World!");
