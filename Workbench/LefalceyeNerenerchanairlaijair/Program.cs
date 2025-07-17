// See https://aka.ms/new-console-template for more information

var second = TimeSpan.FromMinutes(15).TotalSeconds;
var value = Random.Shared.Next(0, (int)second);

Console.WriteLine(TimeSpan.FromSeconds(value).TotalMinutes);
