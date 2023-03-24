// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.RecognizerContext
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class RecognizerContext : ICloneable, IDisposable
  {
    internal InkRecognizerContextPrivate m_Context;
    private RecognizerContextRecognitionEventHandler onRecognition;
    private RecognizerContextRecognitionWithAlternatesEventHandler onRecognitionWithAlternates;
    private bool disposed;

    internal RecognizerContext(InkRecognizerContextPrivate context) => this.m_Context = context;

    public RecognizerContext()
    {
      try
      {
        this.m_Context = (InkRecognizerContextPrivate) new InkRecognizerContextClass();
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    protected virtual void Dispose(bool disposing)
    {
      if (!this.disposed && disposing)
      {
        if (this.m_Context != null)
        {
          IntSecurity.RemoveComEventHandler.Assert();
          if (this.onRecognition != null)
          {
            // ISSUE: method pointer
            this.m_Context.remove_Recognition(new _IInkRecognitionEvents_RecognitionEventHandler((object) this, (UIntPtr) __methodptr(m_Context_Recognition)));
          }
          if (this.onRecognitionWithAlternates != null)
          {
            // ISSUE: method pointer
            this.m_Context.remove_RecognitionWithAlternates(new _IInkRecognitionEvents_RecognitionWithAlternatesEventHandler((object) this, (UIntPtr) __methodptr(m_Context_RecognitionWithAlternates)));
          }
          Marshal.ReleaseComObject((object) this.m_Context);
        }
        this.m_Context = (InkRecognizerContextPrivate) null;
      }
      this.disposed = true;
    }

    ~RecognizerContext() => this.Dispose(false);

    public event RecognizerContextRecognitionEventHandler Recognition
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onRecognition += value;
        if (value == null || this.onRecognition.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Context.add_Recognition(new _IInkRecognitionEvents_RecognitionEventHandler((object) this, (UIntPtr) __methodptr(m_Context_Recognition)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onRecognition == null)
          return;
        this.onRecognition -= value;
        if (this.onRecognition != null || this.m_Context == null)
          return;
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Context.remove_Recognition(new _IInkRecognitionEvents_RecognitionEventHandler((object) this, (UIntPtr) __methodptr(m_Context_Recognition)));
      }
    }

    public event RecognizerContextRecognitionWithAlternatesEventHandler RecognitionWithAlternates
    {
      [MethodImpl(MethodImplOptions.Synchronized)] add
      {
        this.onRecognitionWithAlternates += value;
        if (value == null || this.onRecognitionWithAlternates.GetInvocationList().GetLength(0) != value.GetInvocationList().GetLength(0))
          return;
        IntSecurity.AddComEventHandler.Assert();
        this.m_Context.add_RecognitionWithAlternates(new _IInkRecognitionEvents_RecognitionWithAlternatesEventHandler((object) this, (UIntPtr) __methodptr(m_Context_RecognitionWithAlternates)));
      }
      [MethodImpl(MethodImplOptions.Synchronized)] remove
      {
        if (this.onRecognitionWithAlternates == null)
          return;
        this.onRecognitionWithAlternates -= value;
        if (this.onRecognitionWithAlternates != null || this.m_Context == null)
          return;
        IntSecurity.RemoveComEventHandler.Assert();
        this.m_Context.remove_RecognitionWithAlternates(new _IInkRecognitionEvents_RecognitionWithAlternatesEventHandler((object) this, (UIntPtr) __methodptr(m_Context_RecognitionWithAlternates)));
      }
    }

    object ICloneable.Clone() => (object) this.Clone();

    public RecognizerContext Clone()
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      return new RecognizerContext(this.m_Context.Clone());
    }

    public bool IsStringSupported(string s)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      return this.m_Context.IsStringSupported(s);
    }

    public WordList WordList
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_Context.WordList == null ? (WordList) null : new WordList(this.m_Context.WordList);
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          if (value == null)
            this.m_Context.WordList = (InkWordList) null;
          else
            this.m_Context.WordList = value.m_WordList;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public string PrefixText
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_Context.PrefixText;
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          this.m_Context.PrefixText = value;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public string SuffixText
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_Context.SuffixText;
      }
      set
      {
        try
        {
          if (this.disposed)
            throw new ObjectDisposedException(this.GetType().FullName);
          this.m_Context.SuffixText = value;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public Strokes Strokes
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return (IInkStrokes) this.m_Context.Strokes == null ? (Strokes) null : new Strokes(this.m_Context.Strokes);
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        if (value == null)
          this.m_Context.Strokes = (InkStrokes) null;
        else
          this.m_Context.Strokes = value.m_Strokes;
      }
    }

    public Recognizer Recognizer
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return new Recognizer(this.m_Context.Recognizer);
      }
    }

    public RecognitionModes RecognitionFlags
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return (RecognitionModes) this.m_Context.RecognitionFlags;
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          this.m_Context.RecognitionFlags = (InkRecognitionModes) value;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public RecognizerGuide Guide
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_Context.Guide == null ? new RecognizerGuide(0, 0, 0, new Rectangle(0, 0, 0, 0), new Rectangle(0, 0, 0, 0)) : new RecognizerGuide(this.m_Context.Guide);
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          this.m_Context.Guide = (InkRecognizerGuide) value._InternalCopy();
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public string Factoid
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return this.m_Context.Factoid;
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        try
        {
          this.m_Context.Factoid = value;
        }
        catch (COMException ex)
        {
          InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
          throw;
        }
      }
    }

    public UnicodeRange[] GetEnabledUnicodeRanges()
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      int[] enabledUnicodeRanges;
      try
      {
        enabledUnicodeRanges = (int[]) ((IInkRecognizerContext2) this.m_Context).EnabledUnicodeRanges;
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
      return InkHelperMethods.IntToRanges(enabledUnicodeRanges);
    }

    public void SetEnabledUnicodeRanges(UnicodeRange[] ranges)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      try
      {
        ((IInkRecognizerContext2) this.m_Context).EnabledUnicodeRanges = (object) InkHelperMethods.RangesToInt(ranges);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public RecognizerCharacterAutoCompletionMode CharacterAutoCompletion
    {
      get
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        return (RecognizerCharacterAutoCompletionMode) this.m_Context.CharacterAutoCompletionMode;
      }
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        this.m_Context.CharacterAutoCompletionMode = (InkRecognizerCharacterAutoCompletionMode) value;
      }
    }

    public void StopBackgroundRecognition()
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      this.m_Context.StopBackgroundRecognition();
    }

    public void BackgroundRecognize(object customData)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      try
      {
        this.m_Context.BackgroundRecognize(customData);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
    }

    public void BackgroundRecognize() => this.BackgroundRecognize((object) null);

    public RecognitionResult Recognize(out RecognitionStatus recognitionStatus)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      InkRecognitionStatusPrivate RecognitionStatus = InkRecognitionStatusPrivate.IRS_NoError;
      RecognitionResult recognitionResult1 = (RecognitionResult) null;
      try
      {
        IInkRecognitionResult recognitionResult2 = this.m_Context.Recognize(ref RecognitionStatus);
        if (recognitionResult2 != null)
          recognitionResult1 = new RecognitionResult(recognitionResult2);
      }
      catch (COMException ex)
      {
        InkErrors.ThrowExceptionForInkError(ex.ErrorCode);
        throw;
      }
      recognitionStatus = (RecognitionStatus) RecognitionStatus;
      return recognitionResult1;
    }

    public void EndInkInput()
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      this.m_Context.EndInkInput();
    }

    public void BackgroundRecognizeWithAlternates(object customData)
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      this.m_Context.BackgroundRecognizeWithAlternates(customData);
    }

    public void BackgroundRecognizeWithAlternates()
    {
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      this.m_Context.BackgroundRecognizeWithAlternates();
    }

    private void m_Context_Recognition(
      string s,
      object customData,
      InkRecognitionStatusPrivate RecognitionStatus)
    {
      if (this.onRecognition == null)
        return;
      this.onRecognition((object) this, new RecognizerContextRecognitionEventArgs(s, customData, (RecognitionStatus) RecognitionStatus));
    }

    private void m_Context_RecognitionWithAlternates(
      IInkRecognitionResult result,
      object customData,
      InkRecognitionStatusPrivate RecognitionStatus)
    {
      if (this.onRecognitionWithAlternates == null)
        return;
      RecognitionResult result1 = (RecognitionResult) null;
      if (result != null)
        result1 = new RecognitionResult(result);
      this.onRecognitionWithAlternates((object) this, new RecognizerContextRecognitionWithAlternatesEventArgs(result1, customData, (RecognitionStatus) RecognitionStatus));
    }
  }
}
