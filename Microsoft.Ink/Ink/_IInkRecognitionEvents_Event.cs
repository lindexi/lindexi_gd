// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink._IInkRecognitionEvents_Event
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [ComVisible(false)]
  [TypeLibType(16)]
  [ComEventInterface(typeof (_IInkRecognitionEvents), typeof (_IInkRecognitionEvents_EventProvider))]
  internal interface _IInkRecognitionEvents_Event
  {
    event _IInkRecognitionEvents_RecognitionWithAlternatesEventHandler RecognitionWithAlternates;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_RecognitionWithAlternates(
      _IInkRecognitionEvents_RecognitionWithAlternatesEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_RecognitionWithAlternates(
      _IInkRecognitionEvents_RecognitionWithAlternatesEventHandler A_1);

    event _IInkRecognitionEvents_RecognitionEventHandler Recognition;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_Recognition(_IInkRecognitionEvents_RecognitionEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_Recognition(_IInkRecognitionEvents_RecognitionEventHandler A_1);
  }
}
