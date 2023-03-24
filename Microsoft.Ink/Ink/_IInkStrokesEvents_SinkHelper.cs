// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink._IInkStrokesEvents_SinkHelper
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [TypeLibType(TypeLibTypeFlags.FHidden)]
  [ClassInterface(ClassInterfaceType.None)]
  internal sealed class _IInkStrokesEvents_SinkHelper : InkEventForwarder, _IInkStrokesEvents
  {
    public _IInkStrokesEvents_StrokesAddedEventHandler m_StrokesAddedDelegate;
    public _IInkStrokesEvents_StrokesRemovedEventHandler m_StrokesRemovedDelegate;
    public int m_dwCookie;

    public virtual void StrokesAdded(object A_1)
    {
      if (this.m_StrokesAddedDelegate == null)
        return;
      this.m_StrokesAddedDelegate(A_1);
    }

    public virtual void StrokesRemoved(object A_1)
    {
      if (this.m_StrokesRemovedDelegate == null)
        return;
      this.m_StrokesRemovedDelegate(A_1);
    }

    internal _IInkStrokesEvents_SinkHelper()
    {
      this.m_dwCookie = 0;
      this.m_StrokesAddedDelegate = (_IInkStrokesEvents_StrokesAddedEventHandler) null;
      this.m_StrokesRemovedDelegate = (_IInkStrokesEvents_StrokesRemovedEventHandler) null;
    }
  }
}
