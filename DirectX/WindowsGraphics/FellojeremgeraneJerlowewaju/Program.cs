// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using FellojeremgeraneJerlowewaju;

var screenSnapshotProvider = new ScreenSnapshotProvider();
var snapshot = await screenSnapshotProvider.TakeSnapshotAsync();

Process.Start(new ProcessStartInfo(snapshot.FullName)
{
    UseShellExecute = true
});

Console.WriteLine("Hello, World!");
