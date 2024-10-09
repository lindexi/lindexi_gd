// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Text.Json.Nodes;

var json = """
           {"cId":"41040e95-19f0-4674-9a19-41ae9ac675ef","cSize":554661,"cVersion":1,"userId":"2ced2d79de0f401b126ea6a347475c51","userName":"lindexi"} 
           """;

var jsonNode = JsonNode.Parse(json);
var traceId = jsonNode?["cId"]?.ToString();

Console.WriteLine("Hello, World!");
