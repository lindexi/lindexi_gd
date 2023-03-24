// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink._IInkEvents_SinkHelper
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [TypeLibType(TypeLibTypeFlags.FHidden)]
  [ClassInterface(ClassInterfaceType.None)]
  internal sealed class _IInkEvents_SinkHelper : InkEventForwarder, _IInkEvents
  {
    public _IInkEvents_InkAddedEventHandler m_InkAddedDelegate;
    public _IInkEvents_InkDeletedEventHandler m_InkDeletedDelegate;
    public int m_dwCookie;

    public virtual void InkAdded(object A_1)
    {
      if (this.m_InkAddedDelegate == null)
        return;
      this.m_InkAddedDelegate(A_1);
    }

    public virtual void InkDeleted(object A_1)
    {
      if (this.m_InkDeletedDelegate == null)
        return;
      this.m_InkDeletedDelegate(A_1);
    }

    internal _IInkEvents_SinkHelper()
    {
      this.m_dwCookie = 0;
      this.m_InkAddedDelegate = (_IInkEvents_InkAddedEventHandler) null;
      this.m_InkDeletedDelegate = (_IInkEvents_InkDeletedEventHandler) null;
    }
  }
}
