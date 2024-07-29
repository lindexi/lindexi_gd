// See https://aka.ms/new-console-template for more information

using System.Linq;
using HidSharp;

foreach (var hidDevice in HidSharp.DeviceList.Local.GetHidDevices())
{
    Console.WriteLine($"PID={hidDevice.ProductID:X4} VID={hidDevice.VendorID:X4} Path={hidDevice.DevicePath}");

    try
    {
        var serialNumber = hidDevice.GetSerialNumber();
        Console.WriteLine($"SerialNumber={serialNumber}");
    }
    catch
    {
    }

    try
    {
        var manufacturer = hidDevice.GetManufacturer();
        Console.WriteLine($"Manufacturer={manufacturer}");
    }
    catch
    {
    }

    try
    {
        var friendlyName = hidDevice.GetFriendlyName();
        Console.WriteLine($"FriendlyName={friendlyName}");
    }
    catch
    {
    }

    Console.WriteLine($"ReleaseNumber={hidDevice.ReleaseNumber}");
    Console.WriteLine($"ReleaseNumberBcd={hidDevice.ReleaseNumberBcd}");

    try
    {
        var productName = hidDevice.GetProductName();
        Console.WriteLine($"ProductName={productName}");
    }
    catch
    {
    }

    try
    {
        Console.WriteLine($"SerialPorts={string.Join(';', hidDevice.GetSerialPorts())}");
    }
    catch
    {
    }

    try
    {
        var reportDescriptor = hidDevice.GetReportDescriptor();
        foreach (var report in reportDescriptor.Reports)
        {
            Console.WriteLine($"ReportType={report.ReportType};ReportID={report.ReportID}");
        }
    }
    catch
    {
    }

    try
    {
        var fileSystemName = hidDevice.GetFileSystemName();
        Console.WriteLine($"FileSystemName={fileSystemName}");
    }
    catch
    {
    }

    Console.WriteLine();
}
