// See https://aka.ms/new-console-template for more information

foreach (var driveInfo in DriveInfo.GetDrives())
{
    if (driveInfo.DriveType == DriveType.Removable)
    {
        Console.WriteLine($"{driveInfo.RootDirectory} 是 U 盘");
    }
}
Console.WriteLine("Hello, World!");
