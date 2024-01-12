// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var manualResetEvent = new ManualResetEvent(false);

var str = "The Stupid Code";

unsafe
{
    fixed (char* p = str)
    {
        StupidCode(new IntPtr(p).ToInt64());
    }
}

Task.Run(() =>
{
    manualResetEvent.WaitOne();
});

manualResetEvent.WaitOne();


void StupidCode(long p)
{
    while (p != 0)
    {

    }
}