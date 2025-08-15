using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace FafurbaliHerekaylarnerecerne;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("D8F015C0-C278-11CE-A49E-444553540000")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public unsafe partial interface IShellDispatch : IDispatch
{
    [PreserveSig]
    HRESULT Application(TodoArguments todo);
    [PreserveSig]
    HRESULT Parent(TodoArguments todo);

    [PreserveSig]
    HRESULT NameSpace(TodoArguments todo);
    [PreserveSig]
    HRESULT BrowseForFolder(TodoArguments todo);
    [PreserveSig]
    HRESULT Windows(TodoArguments todo);
    [PreserveSig]
    HRESULT Open(TodoArguments todo);

    [PreserveSig]
    HRESULT Explore(TodoArguments todo);

    [helpstring("Minimize all windows")]
    void MinimizeAll();

    [PreserveSig]
    [helpstring("Undo Minimize All")]
    HRESULT UndoMinimizeALL();

    [PreserveSig]
    [helpstring("Bring up the file run")]
    HRESULT FileRun();

    [PreserveSig]
    [helpstring("Cascade Windows")]
    HRESULT CascadeWindows();

    [helpstring("Tile windows vertically")]
    HRESULT TileVertically();

    [PreserveSig]
    [helpstring("Tile windows horizontally")]
    HRESULT TileHorizontally();

    [PreserveSig]
    [helpstring("Exit Windows")]
    HRESULT ShutdownWindows();

    [PreserveSig]
    [helpstring("Suspend the pc")]
    HRESULT Suspend();

    [PreserveSig]
    [helpstring("Eject the pc")]
    HRESULT EjectPC();

    [PreserveSig]
    [helpstring("Bring up the Set time dialog")]
    HRESULT SetTime();

    [PreserveSig]
    [helpstring("Handle Tray properties")]
    HRESULT TrayProperties();

    [PreserveSig]
    [helpstring("Display shell help")]
    HRESULT Help();

    [PreserveSig]
    [helpstring("Find Files")]
    HRESULT FindFiles();

    [PreserveSig]
    [helpstring("Find a computer")]
    HRESULT FindComputer();

    [PreserveSig]
    [helpstring("Refresh the menu")]
    HRESULT RefreshMenu();

    [PreserveSig]
    [helpstring("Run a Control Panel Item")]
    HRESULT ControlPanelItem(TodoArguments todo);
}