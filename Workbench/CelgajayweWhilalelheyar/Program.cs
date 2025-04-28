// See https://aka.ms/new-console-template for more information

using Device.Net;
using Device.Net.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

ILoggerFactory loggerFactory = new NullLoggerFactory();
Guid GUID_DEVCLASS_MONITOR =
new Guid(0x4d36e96e, 0xe325, 0x11ce, 0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18);

var windowsDeviceEnumerator = new WindowsDeviceEnumerator(new Logger<WindowsDeviceEnumerator>(loggerFactory), GUID_DEVCLASS_MONITOR,
    (id, guid) =>
    {
        return default;
    }, definition =>
    {
        return default;
    });

foreach (ConnectedDeviceDefinition connectedDeviceDefinition in await windowsDeviceEnumerator.GetConnectedDeviceDefinitionsAsync())
{
    
}

Console.WriteLine("Hello, World!");
