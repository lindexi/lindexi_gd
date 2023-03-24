// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.StylusPluginCollectionBase
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using Microsoft.Ink;
using System;
using System.Collections;
using System.Security.Permissions;

namespace Microsoft.StylusInput
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  internal class StylusPluginCollectionBase : CollectionBase
  {
    private Validate m_Validate;
    private ItemInsert m_OnInsert;
    private ItemRemove m_OnRemove;
    private ListClear m_OnClear;
    private ItemSet m_OnSet;
    private ItemInserted m_OnInsertComplete;

    internal StylusPluginCollectionBase(
      Validate validate,
      ItemInsert onAdd,
      ItemInserted onAddComplete,
      ItemRemove onRemove,
      ListClear onClear,
      ItemSet onSet)
    {
      this.m_Validate = validate;
      this.m_OnInsert = onAdd;
      this.m_OnRemove = onRemove;
      this.m_OnClear = onClear;
      this.m_OnSet = onSet;
      this.m_OnInsertComplete = onAddComplete;
    }

    protected override void OnInsert(int index, object item)
    {
      if (this.List.Contains(item))
        throw new ArgumentException(Helpers.SharedResources.Errors.GetString("DuplicateObjectInCollection"));
      this.m_Validate();
      base.OnInsert(index, item);
      if (this.m_OnInsert == null)
        return;
      this.m_OnInsert(index, item);
    }

    protected override void OnInsertComplete(int index, object item)
    {
      try
      {
        base.OnInsertComplete(index, item);
        if (this.m_OnInsertComplete == null)
          return;
        this.m_OnInsertComplete(index, item);
      }
      catch
      {
        this.InnerList.Remove((object) index);
        throw;
      }
    }

    protected override void OnRemove(int index, object item)
    {
      base.OnRemove(index, item);
      this.m_Validate();
      this.m_OnRemove(index, item);
    }

    protected override void OnSet(int index, object oldValue, object newValue)
    {
      if (this.List.Contains(newValue) || oldValue == newValue)
        throw new ArgumentException(Helpers.SharedResources.Errors.GetString("DuplicateObjectInCollection"));
      base.OnSet(index, oldValue, newValue);
      this.m_Validate();
      this.m_OnSet(index, oldValue, newValue);
    }

    protected override void OnClear()
    {
      base.OnClear();
      this.m_Validate();
      this.m_OnClear();
    }

    internal IList CollectionList => this.List;
  }
}
