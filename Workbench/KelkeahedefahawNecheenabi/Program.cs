// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;

var foo = new Foo(2, "zxc");

var gcHandle = GCHandle.Alloc(foo, GCHandleType.Normal);
var intPtr = GCHandle.ToIntPtr(gcHandle);
var gcHandleFromIntPtr = GCHandle.FromIntPtr(intPtr);
if (ReferenceEquals(foo, gcHandleFromIntPtr.Target))
{

}

Console.WriteLine("Hello, World!");

record Foo(int Count, string Name)
{

}