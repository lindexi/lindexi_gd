// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkEdit
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows.Forms;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class InkEdit : RichTextBox
  {
    private InkInsertMode m_inkInsertMode;
    private InkMode m_inkMode = InkMode.InkAndGesture;
    private DrawingAttributes m_DrawingAttributes;
    private int m_recoTimeout = 2000;
    private Recognizer m_Recognizer;
    private string m_sFactoid = Microsoft.Ink.Factoid.Default;
    private bool m_useMouseForInput;
    private ArrayList m_gestureStatusArray = new ArrayList();
    private System.Windows.Forms.Cursor m_UseCursor;
    private bool m_disposing;
    private static IntPtr moduleHandle = IntPtr.Zero;

    [SRCategory("CategoryInk")]
    [SRDescription("InkEditEventStroke")]
    public event InkEditStrokeEventHandler Stroke;

    [SRDescription("InkEditEventRecognition")]
    [SRCategory("CategoryInk")]
    public event InkEditRecognitionEventHandler Recognition;

    [SRCategory("CategoryInk")]
    [SRDescription("InkEditEventGesture")]
    public event InkEditGestureEventHandler Gesture;

    [DefaultValue(InkMode.InkAndGesture)]
    [SRCategory("CategoryInk")]
    [Browsable(true)]
    [SRDescription("InkEditPropertyInkMode")]
    public InkMode InkMode
    {
      [MethodImpl(MethodImplOptions.Synchronized)] get => this.m_inkMode;
      [MethodImpl(MethodImplOptions.Synchronized)] set
      {
        if (this.m_inkMode == value)
          return;
        if (!this.DesignMode && this.IsHandleCreated)
        {
          IntPtr num = UnsafeNativeMethods.SendMessage(this.Handle, 1538, (IntPtr) (long) value, IntPtr.Zero);
          InkHelperMethods.ThrowExceptionForHR(num.ToInt32());
          num = UnsafeNativeMethods.SendMessage(this.Handle, 1537, IntPtr.Zero, IntPtr.Zero);
          value = (InkMode) num.ToInt32();
        }
        this.m_inkMode = value;
        this.Cursor = this.m_UseCursor;
      }
    }

    [DefaultValue(2000)]
    [SRCategory("CategoryInk")]
    [SRDescription("InkEditPropertyRecognitionTimeout")]
    [Browsable(true)]
    public int RecoTimeout
    {
      [MethodImpl(MethodImplOptions.Synchronized)] get => this.m_recoTimeout;
      [MethodImpl(MethodImplOptions.Synchronized)] set
      {
        if (value < 0)
          throw new ArgumentException((string) null, nameof (value));
        if (this.m_recoTimeout == value)
          return;
        if (!this.DesignMode && this.IsHandleCreated)
          InkHelperMethods.ThrowExceptionForHR(UnsafeNativeMethods.SendMessage(this.Handle, 1544, (IntPtr) value, IntPtr.Zero).ToInt32());
        this.m_recoTimeout = value;
      }
    }

    [SRCategory("CategoryInk")]
    [SRDescription("InkEditPropertyInkInsertMode")]
    [Browsable(true)]
    [DefaultValue(InkInsertMode.InsertAsText)]
    public InkInsertMode InkInsertMode
    {
      [MethodImpl(MethodImplOptions.Synchronized)] get => this.m_inkInsertMode;
      [MethodImpl(MethodImplOptions.Synchronized)] set
      {
        if (this.m_inkInsertMode == value)
          return;
        if (!this.DesignMode && this.IsHandleCreated)
          InkHelperMethods.ThrowExceptionForHR(UnsafeNativeMethods.SendMessage(this.Handle, 1540, (IntPtr) (long) value, IntPtr.Zero).ToInt32());
        this.m_inkInsertMode = value;
      }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [SRDescription("InkEditPropertyDrawingAttributes")]
    [SRCategory("CategoryInk")]
    public DrawingAttributes DrawingAttributes
    {
      get
      {
        lock (this)
          return this.m_DrawingAttributes;
      }
      set
      {
        lock (this)
        {
          if (value == null)
            throw new ArgumentNullException(nameof (value), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
          if (!this.DesignMode && this.IsHandleCreated)
          {
            InkDrawingAttributes drawingAttributes = (InkDrawingAttributes) value.m_DrawingAttributes;
            InkHelperMethods.ThrowExceptionForHR(UnsafeNativeMethods.SendMessage(this.Handle, 1542, IntPtr.Zero, drawingAttributes).ToInt32());
          }
          this.m_DrawingAttributes = value;
        }
      }
    }

    [SRCategory("CategoryInk")]
    [SRDescription("InkEditPropertyRecognizer")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public Recognizer Recognizer
    {
      get
      {
        lock (this)
          return this.m_Recognizer;
      }
      set
      {
        lock (this)
        {
          if (value == null)
            throw new ArgumentNullException(nameof (value), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
          if (!this.DesignMode && this.IsHandleCreated)
          {
            IInkRecognizer recognizer = value.m_Recognizer;
            InkHelperMethods.ThrowExceptionForHR(UnsafeNativeMethods.SendMessage(this.Handle, 1548, IntPtr.Zero, recognizer).ToInt32());
          }
          this.m_Recognizer = value;
        }
      }
    }

    [SRCategory("CategoryInk")]
    [Browsable(true)]
    [SRDescription("InkEditPropertyFactoid")]
    [DefaultValue("DEFAULT")]
    public string Factoid
    {
      [MethodImpl(MethodImplOptions.Synchronized)] get => this.m_sFactoid;
      [MethodImpl(MethodImplOptions.Synchronized)] set
      {
        if (!this.DesignMode && this.IsHandleCreated)
          InkHelperMethods.ThrowExceptionForHR(UnsafeNativeMethods.SendMessage(this.Handle, 1550, IntPtr.Zero, value).ToInt32());
        this.m_sFactoid = value;
      }
    }

    [SRCategory("CategoryInk")]
    [Browsable(false)]
    [SRDescription("InkEditPropertySelInks")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Microsoft.Ink.Ink[] SelInks
    {
      get
      {
        object lParam = new object();
        InkHelperMethods.ThrowExceptionForHR(UnsafeNativeMethods.SendMessage(this.Handle, 1551, IntPtr.Zero, ref lParam).ToInt32());
        Array array = (Array) lParam;
        Microsoft.Ink.Ink[] selInks = new Microsoft.Ink.Ink[array.Length];
        for (int index = 0; index < selInks.Length; ++index)
        {
          Microsoft.Ink.Ink ink = new Microsoft.Ink.Ink((InkDisp) array.GetValue(index));
          selInks.SetValue((object) ink, index);
        }
        return selInks;
      }
      set
      {
        object lParam = (object) null;
        if (value != null)
        {
          Microsoft.Ink.Ink[] inkArray = value;
          InkDisp[] nkDispArray = new InkDisp[inkArray.Length];
          for (int index = 0; index < inkArray.Length; ++index)
            nkDispArray[index] = inkArray[index].m_Ink;
          if (inkArray.Length > 0)
            lParam = (object) nkDispArray;
        }
        InkHelperMethods.ThrowExceptionForHR(UnsafeNativeMethods.SendMessage(this.Handle, 1552, IntPtr.Zero, ref lParam).ToInt32());
      }
    }

    [SRCategory("CategoryInk")]
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [SRDescription("InkEditPropertySelInksDisplayMode")]
    public InkDisplayMode SelInksDisplayMode
    {
      get => (InkDisplayMode) UnsafeNativeMethods.SendMessage(this.Handle, 1562, IntPtr.Zero, IntPtr.Zero).ToInt32();
      set => InkHelperMethods.ThrowExceptionForHR(UnsafeNativeMethods.SendMessage(this.Handle, 1561, IntPtr.Zero, (IntPtr) (long) value).ToInt32());
    }

    [DefaultValue(false)]
    [SRDescription("InkEditPropertyUseMouseForInput")]
    [Browsable(true)]
    [SRCategory("CategoryInk")]
    public bool UseMouseForInput
    {
      get => this.m_useMouseForInput;
      set
      {
        if (value == this.m_useMouseForInput)
          return;
        if (!this.DesignMode && this.IsHandleCreated)
          InkHelperMethods.ThrowExceptionForHR(UnsafeNativeMethods.SendMessage(this.Handle, 1560, (IntPtr) Convert.ToInt32(value), IntPtr.Zero).ToInt32());
        this.m_useMouseForInput = value;
      }
    }

    [SRCategory("CategoryInk")]
    [SRDescription("InkEditMethodSetGestureStatus")]
    public void SetGestureStatus(ApplicationGesture gesture, bool listening)
    {
      if (gesture == ApplicationGesture.AllGestures)
        throw new ArgumentException((string) null, nameof (gesture));
      if (!this.DesignMode && this.IsHandleCreated)
        InkHelperMethods.ThrowExceptionForHR(UnsafeNativeMethods.SendMessage(this.Handle, 1546, (IntPtr) (long) gesture, (IntPtr) Convert.ToInt32(listening)).ToInt32());
      bool flag = false;
      foreach (InkEdit.GestureStatus gestureStatus in this.m_gestureStatusArray)
      {
        if (gestureStatus.m_Gesture == gesture)
        {
          gestureStatus.m_Listening = listening;
          flag = true;
          break;
        }
      }
      if (flag)
        return;
      this.m_gestureStatusArray.Add((object) new InkEdit.GestureStatus(gesture, listening));
    }

    [SRCategory("CategoryInk")]
    [SRDescription("InkEditMethodGetGestureStatus")]
    public bool GetGestureStatus(ApplicationGesture gesture)
    {
      if (gesture == ApplicationGesture.AllGestures)
        throw new ArgumentException((string) null, nameof (gesture));
      foreach (InkEdit.GestureStatus gestureStatus in this.m_gestureStatusArray)
      {
        if (gestureStatus.m_Gesture == gesture)
          return gestureStatus.m_Listening;
      }
      switch (gesture)
      {
        case ApplicationGesture.Left:
        case ApplicationGesture.Right:
        case ApplicationGesture.UpRightLong:
        case ApplicationGesture.DownLeftLong:
        case ApplicationGesture.UpRight:
        case ApplicationGesture.DownLeft:
          return true;
        default:
          return false;
      }
    }

    [SRCategory("CategoryInk")]
    [SRDescription("InkEditPropertyCursor")]
    [Browsable(true)]
    public override System.Windows.Forms.Cursor Cursor
    {
      get => base.Cursor;
      set
      {
        if (base.Cursor != value)
        {
          this.m_UseCursor = value;
          if (this.m_inkMode == InkMode.Disabled && value == System.Windows.Forms.Cursors.Default && !this.DesignMode)
          {
            base.Cursor = System.Windows.Forms.Cursors.IBeam;
            base.Cursor = (System.Windows.Forms.Cursor) null;
          }
          else
            base.Cursor = value;
        }
        if (!this.IsHandleCreated || this.DesignMode)
          return;
        IntPtr zero = IntPtr.Zero;
        IntPtr num;
        if (base.Cursor == System.Windows.Forms.Cursors.Default || base.Cursor == (System.Windows.Forms.Cursor) null)
        {
          num = UnsafeNativeMethods.SendMessage(this.Handle, 1556, IntPtr.Zero, (IntPtr) 0L);
          if (num.ToInt32() == 0)
            num = UnsafeNativeMethods.SendMessage(this.Handle, 1554, IntPtr.Zero, IntPtr.Zero);
        }
        else
        {
          num = UnsafeNativeMethods.SendMessage(this.Handle, 1554, IntPtr.Zero, base.Cursor.Handle);
          if (num.ToInt32() == 0)
            num = UnsafeNativeMethods.SendMessage(this.Handle, 1556, IntPtr.Zero, (IntPtr) 99L);
        }
        InkHelperMethods.ThrowExceptionForHR(num.ToInt32());
      }
    }

    [SRCategory("CategoryInk")]
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [SRDescription("InkEditPropertyStatus")]
    public InkEditStatus Status
    {
      [MethodImpl(MethodImplOptions.Synchronized)] get => !this.DesignMode && this.IsHandleCreated ? (InkEditStatus) UnsafeNativeMethods.SendMessage(this.Handle, 1557, IntPtr.Zero, IntPtr.Zero).ToInt32() : InkEditStatus.Idle;
    }

    [SRCategory("CategoryInk")]
    [SRDescription("InkEditMethodRecognize")]
    public void Recognize() => InkHelperMethods.ThrowExceptionForHR(UnsafeNativeMethods.SendMessage(this.Handle, 1558, IntPtr.Zero, IntPtr.Zero).ToInt32());

    protected virtual void OnStroke(InkEditStrokeEventArgs e)
    {
      if (this.Stroke == null)
        return;
      this.Stroke((object) this, e);
    }

    protected virtual void OnRecognition(InkEditRecognitionEventArgs e)
    {
      if (this.Recognition == null)
        return;
      this.Recognition((object) this, e);
    }

    protected virtual void OnGesture(InkEditGestureEventArgs e)
    {
      if (this.Gesture == null)
        return;
      this.Gesture((object) this, e);
    }

    public InkEdit() => this.m_UseCursor = System.Windows.Forms.Cursors.Default;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    [Browsable(false)]
    public new bool Disposing => this.m_disposing || base.Disposing;

    protected override void Dispose(bool disposing)
    {
      if (this.IsDisposed)
        return;
      if (this.Disposing)
        return;
      try
      {
        this.m_disposing = true;
        if (!disposing)
          return;
        try
        {
          this.InkMode = InkMode.Disabled;
        }
        catch (InvalidOperationException ex)
        {
        }
        finally
        {
          this.m_UseCursor = (System.Windows.Forms.Cursor) null;
          this.m_DrawingAttributes = (DrawingAttributes) null;
          this.m_Recognizer = (Recognizer) null;
        }
      }
      finally
      {
        this.m_disposing = false;
        base.Dispose(disposing);
      }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
      base.OnHandleCreated(e);
      if (!this.IsHandleCreated)
        return;
      if (!this.DesignMode)
      {
        IntPtr num1 = UnsafeNativeMethods.SendMessage(this.Handle, 1538, (IntPtr) 0L, IntPtr.Zero);
        InkHelperMethods.ThrowExceptionForHR(num1.ToInt32());
        if (this.m_DrawingAttributes == null)
        {
          InkDrawingAttributes lParam = (InkDrawingAttributes) null;
          num1 = UnsafeNativeMethods.SendMessage(this.Handle, 1541, IntPtr.Zero, ref lParam);
          InkHelperMethods.ThrowExceptionForHR(num1.ToInt32());
          this.m_DrawingAttributes = new DrawingAttributes(new _InternalDrawingAttributes((IInkDrawingAttributes) lParam));
        }
        else
        {
          InkDrawingAttributes drawingAttributes = (InkDrawingAttributes) this.m_DrawingAttributes.m_DrawingAttributes;
          num1 = UnsafeNativeMethods.SendMessage(this.Handle, 1542, IntPtr.Zero, drawingAttributes);
          InkHelperMethods.ThrowExceptionForHR(num1.ToInt32());
        }
        if (this.m_Recognizer == null)
        {
          IInkRecognizer lParam = (IInkRecognizer) null;
          num1 = UnsafeNativeMethods.SendMessage(this.Handle, 1547, IntPtr.Zero, ref lParam);
          InkHelperMethods.ThrowExceptionForHR(num1.ToInt32());
          this.m_Recognizer = new Recognizer(lParam);
        }
        else
        {
          IInkRecognizer recognizer = this.m_Recognizer.m_Recognizer;
          num1 = UnsafeNativeMethods.SendMessage(this.Handle, 1548, IntPtr.Zero, recognizer);
          InkHelperMethods.ThrowExceptionForHR(num1.ToInt32());
        }
        if (base.Cursor == System.Windows.Forms.Cursors.Default || base.Cursor == (System.Windows.Forms.Cursor) null)
        {
          num1 = UnsafeNativeMethods.SendMessage(this.Handle, 1556, IntPtr.Zero, (IntPtr) 0L);
          if (num1.ToInt32() == 0)
            num1 = UnsafeNativeMethods.SendMessage(this.Handle, 1554, IntPtr.Zero, IntPtr.Zero);
        }
        else
        {
          num1 = UnsafeNativeMethods.SendMessage(this.Handle, 1554, IntPtr.Zero, base.Cursor.Handle);
          if (num1.ToInt32() == 0)
            num1 = UnsafeNativeMethods.SendMessage(this.Handle, 1556, IntPtr.Zero, (IntPtr) 99L);
        }
        InkHelperMethods.ThrowExceptionForHR(num1.ToInt32());
        foreach (InkEdit.GestureStatus gestureStatus in this.m_gestureStatusArray)
        {
          num1 = UnsafeNativeMethods.SendMessage(this.Handle, 1546, (IntPtr) (long) gestureStatus.m_Gesture, (IntPtr) Convert.ToInt32(gestureStatus.m_Listening));
          InkHelperMethods.ThrowExceptionForHR(num1.ToInt32());
        }
        InkHelperMethods.ThrowExceptionForHR(UnsafeNativeMethods.SendMessage(this.Handle, 1560, (IntPtr) Convert.ToInt32(this.m_useMouseForInput), IntPtr.Zero).ToInt32());
        IntPtr num2 = UnsafeNativeMethods.SendMessage(this.Handle, 1540, (IntPtr) (long) this.m_inkInsertMode, IntPtr.Zero);
        InkHelperMethods.ThrowExceptionForHR(num2.ToInt32());
        num2 = UnsafeNativeMethods.SendMessage(this.Handle, 1544, (IntPtr) this.m_recoTimeout, IntPtr.Zero);
        InkHelperMethods.ThrowExceptionForHR(num2.ToInt32());
        num2 = UnsafeNativeMethods.SendMessage(this.Handle, 1550, IntPtr.Zero, this.m_sFactoid);
        InkHelperMethods.ThrowExceptionForHR(num2.ToInt32());
        num2 = UnsafeNativeMethods.SendMessage(this.Handle, 1538, (IntPtr) (long) this.m_inkMode, IntPtr.Zero);
        InkHelperMethods.ThrowExceptionForHR(num2.ToInt32());
        this.Cursor = this.m_UseCursor;
        num2 = UnsafeNativeMethods.SendMessage(this.Handle, 1537, IntPtr.Zero, IntPtr.Zero);
        this.m_inkMode = (InkMode) num2.ToInt32();
      }
      else
        InkHelperMethods.ThrowExceptionForHR(UnsafeNativeMethods.SendMessage(this.Handle, 1538, (IntPtr) 0L, IntPtr.Zero).ToInt32());
    }

    protected override CreateParams CreateParams
    {
      [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)] get
      {
        if (InkEdit.moduleHandle == IntPtr.Zero)
        {
          InkEdit.moduleHandle = UnsafeNativeMethods.LoadLibrary("INKED.DLL");
          int lastWin32Error = Marshal.GetLastWin32Error();
          if (InkEdit.moduleHandle == IntPtr.Zero)
            throw new Win32Exception(lastWin32Error, string.Format((IFormatProvider) null, Helpers.SharedResources.Errors.GetString("LoadDLLError"), new object[1]
            {
              (object) "INKED.DLL"
            }));
        }
        CreateParams createParams = base.CreateParams;
        createParams.ClassName = "INKEDIT";
        return createParams;
      }
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    [SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
    protected override void WndProc(ref Message m)
    {
      switch (m.Msg)
      {
        case 1082:
          NativeMethods.CHARFORMATA structure1 = (NativeMethods.CHARFORMATA) UnsafeNativeMethods.PtrToStructure(m.LParam, typeof (NativeMethods.CHARFORMATA));
          if (m.WParam.ToInt32() == -1 && (structure1.dwMask & 536870912) == 0)
          {
            structure1.dwMask |= -16777217;
            m.WParam = new IntPtr(1);
            Marshal.StructureToPtr((object) structure1, m.LParam, false);
            break;
          }
          break;
        case 8270:
          switch (((NativeMethods.NMHDR) UnsafeNativeMethods.PtrToStructure(m.LParam, typeof (NativeMethods.NMHDR))).code)
          {
            case 2049:
              NativeMethods.IEC_STROKEINFO structure2 = (NativeMethods.IEC_STROKEINFO) UnsafeNativeMethods.PtrToStructure(m.LParam, typeof (NativeMethods.IEC_STROKEINFO));
              InkEditStrokeEventArgs e1 = new InkEditStrokeEventArgs(new Cursor(structure2.Cursor), new Microsoft.Ink.Stroke(structure2.Stroke), false);
              this.OnStroke(e1);
              m.Result = new IntPtr(Convert.ToInt32(e1.Cancel));
              return;
            case 2050:
              NativeMethods.IEC_GESTUREINFO structure3 = (NativeMethods.IEC_GESTUREINFO) UnsafeNativeMethods.PtrToStructure(m.LParam, typeof (NativeMethods.IEC_GESTUREINFO));
              Cursor cursor = new Cursor(structure3.Cursor);
              Strokes strokes = new Strokes(structure3.Strokes);
              Array gestures1 = (Array) structure3.Gestures;
              Microsoft.Ink.Gesture[] gestures2 = new Microsoft.Ink.Gesture[gestures1.Length];
              for (int index = 0; index < gestures2.Length; ++index)
              {
                Microsoft.Ink.Gesture gesture = new Microsoft.Ink.Gesture((IInkGesture) gestures1.GetValue(index));
                gestures2.SetValue((object) gesture, index);
              }
              bool cancel = false;
              if (gestures2.Length > 0 && gestures2[0].Id == ApplicationGesture.NoGesture)
                cancel = true;
              InkEditGestureEventArgs e2 = new InkEditGestureEventArgs(cursor, strokes, gestures2, cancel);
              this.OnGesture(e2);
              m.Result = new IntPtr(Convert.ToInt32(e2.Cancel));
              return;
            case 2051:
              this.OnRecognition(new InkEditRecognitionEventArgs(new RecognitionResult(((NativeMethods.IEC_RECOGNITIONRESULTINFO) UnsafeNativeMethods.PtrToStructure(m.LParam, typeof (NativeMethods.IEC_RECOGNITIONRESULTINFO))).RecognitionResult)));
              return;
          }
          break;
      }
      base.WndProc(ref m);
    }

    private class GestureStatus
    {
      public ApplicationGesture m_Gesture;
      public bool m_Listening;

      public GestureStatus(ApplicationGesture gesture, bool listening)
      {
        this.m_Gesture = gesture;
        this.m_Listening = listening;
      }
    }
  }
}
