using System.Runtime.InteropServices;

string arch = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
Console.WriteLine($"SMOKE_OK arch={arch} version={Environment.Version} framework={RuntimeInformation.FrameworkDescription}");
Console.WriteLine("CoreLib=" + typeof(object).Assembly.Location);
Console.WriteLine("Host=" + Environment.ProcessPath);
return args.Length == 1 && args[0] == arch && Enumerable.Range(1, 10).Sum() == 55 ? 0 : 1;
