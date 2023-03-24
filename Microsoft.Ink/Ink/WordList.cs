// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.WordList
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Security.Permissions;
using System.Text;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class WordList
  {
    internal InkWordList m_WordList;

    internal WordList(InkWordList wordList) => this.m_WordList = wordList;

    public WordList() => this.m_WordList = (InkWordList) new InkWordListClass();

    public void Add(string s) => this.m_WordList.AddWord(s);

    public void Add(string[] words)
    {
      if (words == null)
        throw new ArgumentNullException(nameof (words), Helpers.SharedResources.Errors.GetString("ArgumentNull_Array"));
      StringBuilder stringBuilder = new StringBuilder();
      foreach (string word in words)
      {
        stringBuilder.Append(word);
        stringBuilder.Append(char.MinValue);
      }
      stringBuilder.Append(char.MinValue);
      ((IInkWordList2) this.m_WordList).AddWords(stringBuilder.ToString());
    }

    public void Remove(string s) => this.m_WordList.RemoveWord(s);

    public void Merge(WordList wl)
    {
      if (wl == null)
        throw new ArgumentNullException(nameof (wl), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      this.m_WordList.Merge(wl.m_WordList);
    }
  }
}
