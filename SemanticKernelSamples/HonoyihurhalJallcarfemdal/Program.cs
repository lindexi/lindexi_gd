// See https://aka.ms/new-console-template for more information

using System.Text.Json;

using VolcEngineSdk;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var arkClient = new ArkClient(key);

Console.Read();