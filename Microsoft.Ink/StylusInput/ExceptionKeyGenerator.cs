// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.ExceptionKeyGenerator
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

namespace Microsoft.StylusInput
{
  internal sealed class ExceptionKeyGenerator
  {
    private static int m_uniqueKey;
    private static object m_lock = new object();

    private ExceptionKeyGenerator()
    {
    }

    internal static int GetUniqueKey()
    {
      lock (ExceptionKeyGenerator.m_lock)
      {
        if (ExceptionKeyGenerator.m_uniqueKey == int.MaxValue)
          ExceptionKeyGenerator.m_uniqueKey = 0;
        ++ExceptionKeyGenerator.m_uniqueKey;
        return ExceptionKeyGenerator.m_uniqueKey;
      }
    }
  }
}
