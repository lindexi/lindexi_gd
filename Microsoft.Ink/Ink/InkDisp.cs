// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkDisp
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [CoClass(typeof (InkDispClass))]
  [Guid("9D398FA0-C4E2-4FCD-9973-975CAAF47EA6")]
  [ComImport]
  internal interface InkDisp : IInkDisp, _IInkEvents_Event
  {
  }
}
