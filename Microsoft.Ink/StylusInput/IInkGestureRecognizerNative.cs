// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.IInkGestureRecognizerNative
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.StylusInput
{
  [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  [SuppressUnmanagedCodeSecurity]
  [Guid("F7E9B72A-A6FA-43fa-A0D8-A0C038B97203")]
  internal interface IInkGestureRecognizerNative
  {
    [return: MarshalAs(UnmanagedType.Bool)]
    bool get_Enabled();

    void set_Enabled([MarshalAs(UnmanagedType.Bool), In] bool enable);

    [return: MarshalAs(UnmanagedType.I4)]
    int get_MaxStrokeCount();

    void set_MaxStrokeCount([MarshalAs(UnmanagedType.I4), In] int count);

    void EnableGestures([In] uint cGestures, [MarshalAs(UnmanagedType.LPArray), In] int[] gestureArray);

    void Reset();
  }
}
