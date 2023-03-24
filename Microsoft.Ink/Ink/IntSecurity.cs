// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.IntSecurity
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Security;
using System.Security.Permissions;
using System.Windows.Forms;

namespace Microsoft.Ink
{
  internal static class IntSecurity
  {
    private static PermissionSet addComEventHandler;
    private static CodeAccessPermission allWindows;
    private static CodeAccessPermission clipboardRead;
    private static CodeAccessPermission clipboardWrite;
    private static CodeAccessPermission memberAccess;
    private static PermissionSet removeComEventHandler;
    private static CodeAccessPermission unmanagedCode;

    public static PermissionSet AddComEventHandler
    {
      get
      {
        if (IntSecurity.addComEventHandler == null)
        {
          IntSecurity.addComEventHandler = new PermissionSet(PermissionState.None);
          IntSecurity.addComEventHandler.SetPermission((IPermission) IntSecurity.UnmanagedCode);
          IntSecurity.addComEventHandler.SetPermission((IPermission) IntSecurity.MemberAccess);
        }
        return IntSecurity.addComEventHandler;
      }
    }

    public static CodeAccessPermission AllWindows
    {
      get
      {
        if (IntSecurity.allWindows == null)
          IntSecurity.allWindows = (CodeAccessPermission) new UIPermission(UIPermissionWindow.AllWindows);
        return IntSecurity.allWindows;
      }
    }

    public static CodeAccessPermission ClipboardRead
    {
      get
      {
        if (IntSecurity.clipboardRead == null)
          IntSecurity.clipboardRead = (CodeAccessPermission) new UIPermission(UIPermissionClipboard.AllClipboard);
        return IntSecurity.clipboardRead;
      }
    }

    public static CodeAccessPermission ClipboardWrite
    {
      get
      {
        if (IntSecurity.clipboardWrite == null)
          IntSecurity.clipboardWrite = (CodeAccessPermission) new UIPermission(UIPermissionClipboard.OwnClipboard);
        return IntSecurity.clipboardWrite;
      }
    }

    public static CodeAccessPermission UnmanagedCode
    {
      get
      {
        if (IntSecurity.unmanagedCode == null)
          IntSecurity.unmanagedCode = (CodeAccessPermission) new SecurityPermission(SecurityPermissionFlag.UnmanagedCode);
        return IntSecurity.unmanagedCode;
      }
    }

    public static CodeAccessPermission MemberAccess
    {
      get
      {
        if (IntSecurity.memberAccess == null)
          IntSecurity.memberAccess = (CodeAccessPermission) new ReflectionPermission(ReflectionPermissionFlag.MemberAccess);
        return IntSecurity.memberAccess;
      }
    }

    public static PermissionSet RemoveComEventHandler
    {
      get
      {
        if (IntSecurity.removeComEventHandler == null)
        {
          IntSecurity.removeComEventHandler = new PermissionSet(PermissionState.None);
          IntSecurity.removeComEventHandler.SetPermission((IPermission) IntSecurity.UnmanagedCode);
        }
        return IntSecurity.removeComEventHandler;
      }
    }

    internal static void DemandPermissionToCollectOnWindow(IntPtr hwnd)
    {
      if (IntPtr.Zero.Equals((object) hwnd) || Control.FromHandle(hwnd) != null)
        return;
      IntSecurity.UnmanagedCode.Demand();
    }
  }
}
