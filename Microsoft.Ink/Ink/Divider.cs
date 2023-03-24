// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.Divider
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class Divider : IDisposable
  {
    internal const int RecognizerNotRegistered = -2147220939;
    internal const int NoStrokesToRecognize = -2144862200;
    internal IntPtr m_hInkDivider;
    internal RecognizerContext m_RecognizerContext;
    internal Strokes m_Strokes;
    internal IntPtr m_hModuleOGL;
    internal SafeNativeMethods.GetLineRecoDef m_callback;
    internal int m_LineHeight = 1200;
    private bool disposed;

    public Divider()
    {
      RegistryKey classesRoot = Registry.ClassesRoot;
      string pathList = classesRoot.Name + "\\CLSID\\{8854F6A0-4683-4AE7-9191-752FE64612C3}\\InprocServer32";
      PermissionSet permissionSet = new PermissionSet(PermissionState.None);
      permissionSet.AddPermission((IPermission) new RegistryPermission(RegistryPermissionAccess.Read, pathList));
      permissionSet.AddPermission((IPermission) new EnvironmentPermission(EnvironmentPermissionAccess.Read, "COMMONPROGRAMFILES"));
      permissionSet.AddPermission((IPermission) new EnvironmentPermission(EnvironmentPermissionAccess.Read, "COMMONPROGRAMFILES(X86)"));
      permissionSet.Assert();
      string libname = (string) classesRoot.OpenSubKey("CLSID\\{8854F6A0-4683-4AE7-9191-752FE64612C3}\\InprocServer32").GetValue("");
      this.m_hModuleOGL = UnsafeNativeMethods.LoadLibrary(libname);
      int lastWin32Error = Marshal.GetLastWin32Error();
      if (IntPtr.Zero == this.m_hModuleOGL)
        throw new Win32Exception(lastWin32Error, string.Format((IFormatProvider) null, Helpers.SharedResources.Errors.GetString("LoadDLLError"), new object[1]
        {
          (object) libname
        }));
      this.m_hInkDivider = SafeNativeMethods.CreateInkDivider();
      if (IntPtr.Zero == this.m_hInkDivider)
        throw new OutOfMemoryException();
      this.m_callback = new SafeNativeMethods.GetLineRecoDef(this.GetLineReco);
      SafeNativeMethods.SetLineRecoCallback(this.m_hInkDivider, this.m_callback);
    }

    ~Divider() => this.Dispose(false);

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    protected virtual void Dispose(bool disposing)
    {
      if (!this.disposed)
      {
        if (disposing)
        {
          this.m_RecognizerContext = (RecognizerContext) null;
          if (this.m_Strokes != null)
          {
            this.m_Strokes.StrokesAdded -= new StrokesEventHandler(this.HandleStrokesAdded);
            this.m_Strokes.StrokesRemoved -= new StrokesEventHandler(this.HandleStrokesRemoved);
          }
          this.m_Strokes = (Strokes) null;
          this.m_callback = (SafeNativeMethods.GetLineRecoDef) null;
        }
        if (IntPtr.Zero != this.m_hInkDivider)
        {
          SafeNativeMethods.DeleteInkDivider(this.m_hInkDivider);
          this.m_hInkDivider = IntPtr.Zero;
        }
        if (IntPtr.Zero != this.m_hModuleOGL)
        {
          UnsafeNativeMethods.FreeLibrary(this.m_hModuleOGL);
          this.m_hModuleOGL = IntPtr.Zero;
        }
      }
      this.disposed = true;
    }

    public Divider(Strokes strokes)
      : this()
    {
      this.Strokes = strokes;
    }

    public Divider(Strokes strokes, RecognizerContext recognizerContext)
      : this()
    {
      this.m_RecognizerContext = recognizerContext;
      if (recognizerContext != null && recognizerContext.Recognizer == null)
        throw new ArgumentException("recognizerContext.Recognizer", Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
      if (recognizerContext != null && (recognizerContext.Recognizer.Capabilities & RecognizerCapabilities.FreeInput) == (RecognizerCapabilities) 0)
        throw new ArgumentException(Helpers.SharedResources.Errors.GetString("RecognizerNotFreeInput"), nameof (recognizerContext));
      if (recognizerContext != null && recognizerContext.Recognizer.Languages.Length == 0)
        throw new ArgumentException(Helpers.SharedResources.Errors.GetString("RecognizerNotLanguageRecognizer"), nameof (recognizerContext));
      if (recognizerContext != null)
        SafeNativeMethods.RecognizerContextSet(this.m_hInkDivider);
      this.Strokes = strokes;
    }

    private void GetLineReco(
      int cStrokes,
      int[] aStrokeIds,
      float degrees,
      Point point,
      out string lineString,
      out int cSegment,
      out int[] StrokeIdOrdered,
      out int[] SegmentStrokes,
      out string[] aSegmentString)
    {
      Strokes strokes1 = this.m_Strokes.Ink.CreateStrokes(aStrokeIds);
      Microsoft.Ink.Ink strokes2 = this.m_Strokes.Ink.ExtractStrokes(strokes1, ExtractFlags.CopyFromOriginal);
      Strokes strokes3 = strokes2.Strokes;
      strokes3.Rotate(degrees, point);
      this.m_RecognizerContext.Strokes = (Strokes) null;
      Rectangle drawnBox = new Rectangle(0, 0, strokes3.GetBoundingBox().Right - strokes3.GetBoundingBox().Left, strokes3.GetBoundingBox().Bottom - strokes3.GetBoundingBox().Bottom);
      this.m_RecognizerContext.Guide = new RecognizerGuide(1, 0, 0, strokes3.GetBoundingBox(), drawnBox);
      this.m_RecognizerContext.Strokes = strokes3;
      this.m_RecognizerContext.EndInkInput();
      RecognitionResult recognitionResult = this.m_RecognizerContext.Recognize(out RecognitionStatus _);
      lineString = recognitionResult.TopString;
      RecognitionAlternates recognitionAlternates = recognitionResult.TopAlternate.AlternatesWithConstantPropertyValues(RecognitionProperty.Segmentation);
      cSegment = 0;
      foreach (RecognitionAlternate recognitionAlternate in recognitionAlternates)
      {
        if (recognitionAlternate.Strokes.Count != 0)
          ++cSegment;
      }
      StrokeIdOrdered = new int[aStrokeIds.GetLength(0)];
      SegmentStrokes = new int[cSegment];
      aSegmentString = new string[cSegment];
      int index1 = 0;
      int index2 = 0;
      foreach (RecognitionAlternate recognitionAlternate in recognitionAlternates)
      {
        if (recognitionAlternate.Strokes.Count != 0)
        {
          SegmentStrokes[index1] = recognitionAlternate.Strokes.Count;
          aSegmentString[index1] = recognitionAlternate.ToString();
          foreach (Stroke stroke in recognitionAlternate.Strokes)
          {
            StrokeIdOrdered[index2] = strokes1[stroke.Id - 1].Id;
            ++index2;
          }
          ++index1;
        }
      }
      this.m_RecognizerContext.Strokes = (Strokes) null;
      strokes3.Dispose();
      strokes1.Dispose();
      strokes2.Dispose();
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public DivisionResult Divide()
    {
      int lineCount = 0;
      int wordCount = 0;
      int paragraphCount = 0;
      if (this.m_Strokes == null)
        throw new ArgumentException(Helpers.SharedResources.Errors.GetString("NoStrokesToRecognize"));
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      int wordSize;
      int lineSize;
      int paragraphSize;
      int drawingSize;
      SafeNativeMethods.CallDivide(this.m_hInkDivider, out wordSize, out lineSize, out paragraphSize, out drawingSize, out wordCount, out lineCount, out paragraphCount);
      int[] numArray1 = new int[wordSize];
      int[] numArray2 = new int[lineSize];
      int[] numArray3 = new int[paragraphSize];
      int[] numArray4 = new int[drawingSize];
      int[] aWordRotationCenterX = new int[wordCount];
      int[] aWordRotationCenterY = new int[wordCount];
      float[] numArray5 = new float[wordCount];
      int[] aLineRotationCenterX = new int[lineCount];
      int[] aLineRotationCenterY = new int[lineCount];
      float[] numArray6 = new float[lineCount];
      string[] astrWords;
      string[] astrLines;
      string[] astrParagraphs;
      SafeNativeMethods.CallDivideResults(this.m_hInkDivider, numArray1, numArray2, numArray3, numArray4, out astrWords, out astrLines, out astrParagraphs, aWordRotationCenterX, aWordRotationCenterY, numArray5, aLineRotationCenterX, aLineRotationCenterY, numArray6);
      return new DivisionResult(this.m_Strokes, numArray1, numArray2, numArray3, numArray4, astrWords, astrLines, astrParagraphs, aWordRotationCenterX, aWordRotationCenterY, numArray5, aLineRotationCenterX, aLineRotationCenterY, numArray6);
    }

    public int LineHeight
    {
      get => this.m_LineHeight;
      set
      {
        if (value > 50000 || value < 100)
          throw new ArgumentOutOfRangeException(nameof (value));
        if (this.m_Strokes != null)
          throw new ArgumentException(Helpers.SharedResources.Errors.GetString("CannotSetLineHeightProperty"));
        this.m_LineHeight = value;
        SafeNativeMethods.SetLineHeight(this.m_hInkDivider, this.m_LineHeight);
      }
    }

    public RecognizerContext RecognizerContext
    {
      get => this.m_RecognizerContext == null ? (RecognizerContext) null : this.m_RecognizerContext;
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        if (this.m_RecognizerContext != null || this.m_Strokes != null)
          throw new ArgumentException(Helpers.SharedResources.Errors.GetString("RecognizerContextAlreadySet"), nameof (value));
        if (value != null && value.Recognizer == null)
          throw new ArgumentException("value.Recognizer", Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
        if (value != null && (value.Recognizer.Capabilities & RecognizerCapabilities.FreeInput) == (RecognizerCapabilities) 0)
          throw new ArgumentException(Helpers.SharedResources.Errors.GetString("RecognizerNotFreeInput"), nameof (value));
        if (value != null && value.Recognizer.Languages.Length == 0)
          throw new ArgumentException(Helpers.SharedResources.Errors.GetString("RecognizerNotLanguageRecognizer"), nameof (value));
        this.m_RecognizerContext = value;
        if (value == null)
          return;
        SafeNativeMethods.RecognizerContextSet(this.m_hInkDivider);
      }
    }

    public Strokes Strokes
    {
      get => this.m_Strokes == null ? (Strokes) null : this.m_Strokes;
      set
      {
        if (this.disposed)
          throw new ObjectDisposedException(this.GetType().FullName);
        if (this.m_Strokes != null)
          throw new ArgumentException(Helpers.SharedResources.Errors.GetString("StrokesAlreadySet"));
        if (value == null)
        {
          this.m_Strokes = (Strokes) null;
        }
        else
        {
          this.m_Strokes = value;
          foreach (Stroke stroke in this.m_Strokes)
          {
            Point[] points = stroke.GetPoints();
            SafeNativeMethods.AddOneStroke(this.m_hInkDivider, stroke.Id, points.GetLength(0), points);
          }
          this.m_Strokes.StrokesAdded += new StrokesEventHandler(this.HandleStrokesAdded);
          this.m_Strokes.StrokesRemoved += new StrokesEventHandler(this.HandleStrokesRemoved);
        }
      }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    private void HandleStrokesAdded(object o, StrokesEventArgs arg)
    {
      int[] strokeIds = arg.StrokeIds;
      Strokes strokes = this.m_Strokes.Ink.CreateStrokes(strokeIds);
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      for (int index = 0; index < strokes.Count; ++index)
      {
        Point[] points = strokes[index].GetPoints();
        SafeNativeMethods.AddOneStroke(this.m_hInkDivider, strokeIds[index], points.GetLength(0), points);
      }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    private void HandleStrokesRemoved(object o, StrokesEventArgs arg)
    {
      int[] strokeIds = arg.StrokeIds;
      if (this.disposed)
        throw new ObjectDisposedException(this.GetType().FullName);
      SafeNativeMethods.RemoveStrokes(this.m_hInkDivider, strokeIds.GetLength(0), strokeIds);
    }
  }
}
