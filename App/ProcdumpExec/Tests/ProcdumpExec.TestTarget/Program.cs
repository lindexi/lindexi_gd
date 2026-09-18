using System.Runtime.InteropServices;

if (args is ["crash"])
{
    Marshal.WriteInt32(0, 42);
}

if (args is ["exit", var exitCode])
{
    return int.Parse(exitCode);
}

Console.WriteLine(string.Join('|', args));
return 0;
