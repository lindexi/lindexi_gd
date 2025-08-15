// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;
using FafurbaliHerekaylarnerecerne;

var shellComObject = new ShellComObject();
var dispatch = shellComObject.As<IShellDispatch>()!;
unsafe
{
    //    __invokeRetVal = ((delegate* unmanaged[MemberFunction]<void*, global::FafurbaliHerekaylarnerecerne.HRESULT*, int> )__vtable_native[10])(__this, &__retVal);
    //  __invokeRetVal = ((delegate* unmanaged[MemberFunction]<void*, int> )__vtable_native[10])(__this);

    dispatch.MinimizeAll();
    dispatch.FileRun();
}


ShellHelper.ToggleDesktop();

var linkFile = @"C:\lindexi\快捷方式.lnk";
Console.WriteLine(ShellHelper.GetLinkTargetPath(new FileInfo(linkFile)));