// See https://aka.ms/new-console-template for more information

using System.Text;

var stringBuilder = new StringBuilder();

foreach (var name in Enum.GetNames<Environment.SpecialFolder>())
{
    stringBuilder.AppendLine($"{name} = {Environment.GetFolderPath(Enum.Parse<Environment.SpecialFolder>(name))}");
}

Console.WriteLine(stringBuilder.ToString());

foreach (var name in new[] { "XDG_DATA_HOME", "XDG_CONFIG_HOME", "XDG_CACHE_HOME" })
{
    stringBuilder.AppendLine($"{name} = {Environment.GetEnvironmentVariable(name)}");
}

File.WriteAllText("output.txt", stringBuilder.ToString());

Console.Read();