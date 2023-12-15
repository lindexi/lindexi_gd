using System.Runtime.InteropServices;

namespace FaryubawkaJebelchako;

public static class Foo
{
    [UnmanagedCallersOnly(EntryPoint = "Add")]
    public static int Add(int a, int b) => a + b;
}