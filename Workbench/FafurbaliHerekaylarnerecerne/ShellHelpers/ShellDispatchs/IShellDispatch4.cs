using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace FafurbaliHerekaylarnerecerne;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("efd84b2d-4bcf-4298-be25-eb542a59fbda")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[ComImport]
public partial interface IShellDispatch4
{

}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("D8F015C0-C278-11CE-A49E-444553540000")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[ComImport]
public unsafe partial interface IShellDispatch
{
    HRESULT Application(IDispatch** ppid);
    HRESULT Parent(IDispatch** ppid);

    HRESULT NameSpace(TodoArguments todo);
    HRESULT BrowseForFolder(TodoArguments todo);
    HRESULT Windows(TodoArguments todo);
    HRESULT Open(TodoArguments todo);

    HRESULT Explore(TodoArguments todo);

    [helpstring("Minimize all windows")]
    HRESULT MinimizeAll();

    [helpstring("Undo Minimize All")]
    HRESULT UndoMinimizeALL();

    [helpstring("Bring up the file run")]
    HRESULT FileRun();

    [helpstring("Cascade Windows")]
    HRESULT CascadeWindows();

    [helpstring("Tile windows vertically")]
    HRESULT TileVertically();

    [helpstring("Tile windows horizontally")]
    HRESULT TileHorizontally();

    [helpstring("Exit Windows")]
    HRESULT ShutdownWindows();

    [helpstring("Suspend the pc")]
    HRESULT Suspend();

    [helpstring("Eject the pc")]
    HRESULT EjectPC();

    [helpstring("Bring up the Set time dialog")]
    HRESULT SetTime();

    [helpstring("Handle Tray properties")]
    HRESULT TrayProperties();

    [helpstring("Display shell help")]
    HRESULT Help();

    [helpstring("Find Files")]
    HRESULT FindFiles();

    [helpstring("Find a computer")]
    HRESULT FindComputer();

    [helpstring("Refresh the menu")]
    HRESULT RefreshMenu();

    [helpstring("Run a Control Panel Item")]
    HRESULT ControlPanelItem(TodoArguments todo);
}

class helpstringAttribute : Attribute
{
    public helpstringAttribute(string description)
    {
        Description = description;
    }
    public string Description { get; }
}

public struct TodoArguments
{
}

public readonly record struct HRESULT(int HResult)
{
    public void ThrowIfNotSuccess()
    {
        if (HResult != 0)
        {
            Marshal.ThrowExceptionForHR(HResult);
        }
    }
}