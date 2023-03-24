// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkHelperMethods
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  internal static class InkHelperMethods
  {
    internal static Point[] CoordsToPoints(int[] coords)
    {
      Point[] points = (Point[]) null;
      if (coords != null)
      {
        int length = coords.Length / 2;
        points = new Point[length];
        for (int index = 0; index < length; ++index)
          points[index] = new Point(coords[2 * index], coords[2 * index + 1]);
      }
      return points;
    }

    internal static int[] PointsToCoords(Point[] points)
    {
      int[] coords = (int[]) null;
      if (points != null)
      {
        int length = points.Length;
        coords = new int[length * 2];
        for (int index = 0; index < length; ++index)
        {
          coords[2 * index] = points[index].X;
          coords[2 * index + 1] = points[index].Y;
        }
      }
      return coords;
    }

    internal static UnicodeRange[] IntToRanges(int[] values)
    {
      UnicodeRange[] ranges = (UnicodeRange[]) null;
      if (values != null)
      {
        int length = values.Length / 2;
        ranges = new UnicodeRange[length];
        for (int index = 0; index < length; ++index)
          ranges[index] = new UnicodeRange((char) values[2 * index], values[2 * index + 1]);
      }
      return ranges;
    }

    internal static int[] RangesToInt(UnicodeRange[] ranges)
    {
      int[] numArray = (int[]) null;
      if (ranges != null)
      {
        int length = ranges.Length;
        numArray = new int[length * 2];
        for (int index = 0; index < length; ++index)
        {
          numArray[2 * index] = (int) ranges[index].StartingCharacter;
          numArray[2 * index + 1] = ranges[index].Length;
        }
      }
      return numArray;
    }

    internal static byte[] TabletPropertyDescriptionToBytes(
      TabletPropertyDescriptionCollection tabletPropertyDescriptionCollection)
    {
      MemoryStream output = new MemoryStream();
      using (BinaryWriter binaryWriter = new BinaryWriter((Stream) output))
      {
        binaryWriter.Write(tabletPropertyDescriptionCollection.InkToDeviceScaleX);
        binaryWriter.Write(tabletPropertyDescriptionCollection.InkToDeviceScaleY);
        int count = tabletPropertyDescriptionCollection.Count;
        binaryWriter.Write(count);
        for (int index = 0; index < count; ++index)
        {
          TabletPropertyDescription propertyDescription = tabletPropertyDescriptionCollection[index];
          binaryWriter.Write(propertyDescription.PacketPropertyId.ToByteArray());
          binaryWriter.Write(propertyDescription.TabletPropertyMetrics.Minimum);
          binaryWriter.Write(propertyDescription.TabletPropertyMetrics.Maximum);
          binaryWriter.Write((int) propertyDescription.TabletPropertyMetrics.Units);
          binaryWriter.Write(propertyDescription.TabletPropertyMetrics.Resolution);
        }
      }
      return output.ToArray();
    }

    internal static void ThrowExceptionForHR(int hresult)
    {
      if (hresult == 0)
        return;
      InkErrors.ThrowExceptionForInkError(hresult);
      Marshal.ThrowExceptionForHR(hresult);
    }

    internal static void ValidateCopyToArray(Array array, int index, int sourceLength)
    {
      if (array == null)
        throw new ArgumentNullException(nameof (array), Helpers.SharedResources.Errors.GetString("ArgumentNull_Array"));
      if (array.Rank != 1)
        throw new ArgumentException(Helpers.SharedResources.Errors.GetString("Arg_RankMultiDimNotSupported"));
      if (index < 0)
        throw new ArgumentOutOfRangeException(nameof (index), Helpers.SharedResources.Errors.GetString("ArgumentOutOfRange_NeedNonNegNum"));
      if (array.Length - index < sourceLength)
        throw new ArgumentException(Helpers.SharedResources.Errors.GetString("Arg_ArrayPlusOffTooSmall"));
    }
  }
}
