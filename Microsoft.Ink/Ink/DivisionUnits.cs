// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.DivisionUnits
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Collections;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace Microsoft.Ink
{
  [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
  public class DivisionUnits : ICollection, IEnumerable
  {
    internal const int InvalidStroke = -2147220958;
    private int m_cUnit;
    private DivisionUnit[] m_DivisionUnit;
    private InkDivisionType m_divisionType;

    internal DivisionUnits(Microsoft.Ink.Ink pInk, InkDivisionType divisionType, int[] aUnitStrokes)
    {
      this.m_divisionType = divisionType;
      this.m_cUnit = aUnitStrokes[0];
      this.m_DivisionUnit = new DivisionUnit[this.m_cUnit];
      int index1 = 1;
      for (int index2 = 0; index2 < this.m_cUnit; ++index2)
      {
        int aUnitStroke = aUnitStrokes[index1];
        ++index1;
        int[] StrokeIds = new int[aUnitStroke];
        for (int index3 = 0; index3 < aUnitStroke; ++index3)
        {
          StrokeIds[index3] = aUnitStrokes[index1];
          ++index1;
        }
        Strokes strokes = DivisionUnits.CreateStrokes(pInk, StrokeIds);
        this.m_DivisionUnit[index2] = new DivisionUnit(strokes, divisionType);
      }
    }

    internal DivisionUnits(
      Microsoft.Ink.Ink pInk,
      InkDivisionType divisionType,
      int[] aUnitStrokes,
      string[] aUnitStrings)
    {
      this.m_divisionType = divisionType;
      this.m_cUnit = aUnitStrokes[0];
      this.m_DivisionUnit = new DivisionUnit[this.m_cUnit];
      int index1 = 1;
      for (int index2 = 0; index2 < this.m_cUnit; ++index2)
      {
        int aUnitStroke = aUnitStrokes[index1];
        ++index1;
        int[] StrokeIds = new int[aUnitStroke];
        for (int index3 = 0; index3 < aUnitStroke; ++index3)
        {
          StrokeIds[index3] = aUnitStrokes[index1];
          ++index1;
        }
        Strokes strokes = DivisionUnits.CreateStrokes(pInk, StrokeIds);
        this.m_DivisionUnit[index2] = new DivisionUnit(strokes, divisionType, aUnitStrings[index2]);
      }
    }

    internal DivisionUnits(
      Microsoft.Ink.Ink pInk,
      InkDivisionType divisionType,
      int[] aUnitStrokes,
      string[] aUnitStrings,
      int[] aRotationCenterX,
      int[] aRotationCenterY,
      float[] aAngle)
    {
      this.m_divisionType = divisionType;
      this.m_cUnit = aUnitStrokes[0];
      this.m_DivisionUnit = new DivisionUnit[this.m_cUnit];
      int index1 = 1;
      Point RotationCenter = new Point();
      for (int index2 = 0; index2 < this.m_cUnit; ++index2)
      {
        int aUnitStroke = aUnitStrokes[index1];
        ++index1;
        int[] StrokeIds = new int[aUnitStroke];
        for (int index3 = 0; index3 < aUnitStroke; ++index3)
        {
          StrokeIds[index3] = aUnitStrokes[index1];
          ++index1;
        }
        Strokes strokes = DivisionUnits.CreateStrokes(pInk, StrokeIds);
        RotationCenter.X = aRotationCenterX[index2];
        RotationCenter.Y = aRotationCenterY[index2];
        this.m_DivisionUnit[index2] = aUnitStrings == null ? new DivisionUnit(strokes, divisionType, (string) null, RotationCenter, aAngle[index2]) : new DivisionUnit(strokes, divisionType, aUnitStrings[index2], RotationCenter, aAngle[index2]);
      }
    }

    private DivisionUnits()
    {
    }

    private static Strokes CreateStrokes(Microsoft.Ink.Ink myInk, int[] StrokeIds)
    {
      try
      {
        return myInk.CreateStrokes(StrokeIds);
      }
      catch (COMException ex1)
      {
        if (-2147220958 == ex1.ErrorCode)
        {
          Strokes strokes1 = myInk.CreateStrokes();
          int[] ids = new int[1];
          foreach (int strokeId in StrokeIds)
          {
            try
            {
              ids[0] = strokeId;
              using (Strokes strokes2 = myInk.CreateStrokes(ids))
                strokes1.Add(strokes2);
            }
            catch (COMException ex2)
            {
              if (-2147220958 != ex2.ErrorCode)
                throw;
            }
          }
          return strokes1;
        }
        throw;
      }
    }

    public object SyncRoot => (object) this;

    public int Count => this.m_cUnit;

    public bool IsSynchronized => true;

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void CopyTo(Array array, int index) => this.m_DivisionUnit.CopyTo(array, index);

    public DivisionUnit this[int index]
    {
      get
      {
        try
        {
          return this.m_DivisionUnit[index];
        }
        catch (ArgumentException ex)
        {
          throw new ArgumentOutOfRangeException(nameof (index));
        }
      }
    }

    public DivisionUnits.InkDivisionUnitsEnumerator GetEnumerator() => new DivisionUnits.InkDivisionUnitsEnumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => (IEnumerator) new DivisionUnits.InkDivisionUnitsEnumerator(this);

    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    public class InkDivisionUnitsEnumerator : IEnumerator
    {
      private int position = -1;
      private DivisionUnit[] a;

      public InkDivisionUnitsEnumerator(DivisionUnits a)
      {
        this.a = a != null ? new DivisionUnit[a.Count] : throw new ArgumentNullException(nameof (a), Helpers.SharedResources.Errors.GetString("ValueCannotBeNull"));
        for (int index = 0; index < this.a.Length; ++index)
          this.a[index] = a[index];
      }

      public bool MoveNext()
      {
        if (this.position >= this.a.Length - 1)
          return false;
        ++this.position;
        return true;
      }

      public void Reset() => this.position = -1;

      object IEnumerator.Current => (object) this.Current;

      public DivisionUnit Current => this.a[this.position];
    }
  }
}
