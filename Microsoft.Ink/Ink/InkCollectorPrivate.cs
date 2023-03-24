// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkCollectorPrivate
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [Guid("F0F060B5-8B1F-4A7C-89EC-880692588A4F")]
  [CoClass(typeof (InkCollectorClass))]
  [ComImport]
  internal interface InkCollectorPrivate : IInkCollector, _IInkCollectorEvents_Event
  {
  }
}
