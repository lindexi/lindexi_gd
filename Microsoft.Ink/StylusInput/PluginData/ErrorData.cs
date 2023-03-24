// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.PluginData.ErrorData
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;

namespace Microsoft.StylusInput.PluginData
{
  public sealed class ErrorData
  {
    private DataInterestMask dataId;
    private object plugin;
    private Exception innerException;

    internal ErrorData(object plugin, DataInterestMask dataId, Exception innerException)
    {
      this.plugin = plugin;
      this.dataId = dataId;
      this.innerException = innerException;
    }

    public DataInterestMask DataId => this.dataId;

    public object Plugin => this.plugin;

    public Exception InnerException => this.innerException;
  }
}
