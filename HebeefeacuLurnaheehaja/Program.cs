// See https://aka.ms/new-console-template for more information
using System.Runtime.InteropServices;

var downloadFolderGuid = new Guid("374DE290-123F-4565-9164-39C4925E467B");

var downloadFolderPath = SHGetKnownFolderPath(downloadFolderGuid, 0, IntPtr.Zero);
Console.WriteLine(downloadFolderPath);
[DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
static extern string SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid id, int flags, IntPtr token);