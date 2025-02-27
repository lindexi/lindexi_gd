//using System;
//using System.Collections.Generic;
//using LightTextEditorPlus.Core.Primitive;

//namespace LightTextEditorPlus.Core.Layout;

///// <summary>
///// 单个字符的行布局结果
///// </summary>
//public readonly struct SingleCharInLineLayoutResult
//{
//    /// <summary>
//    /// 单个字符的行布局结果
//    /// </summary>
//    /// <param name="takeCharCount">所采用的字符数量</param>
//    /// <param name="totalSize">总的尺寸</param>
//    /// <param name="charSizeList">各个字符的尺寸。如果采用的字符数量是 1 个时，此属性可以为空，因为字符的尺寸等于 <paramref name="totalSize"/> 尺寸</param>
//    public SingleCharInLineLayoutResult(int takeCharCount, LineCharSize totalSize,IReadOnlyList<LineCharSize>? charSizeList=null)
//    {
//       TakeCharCount = takeCharCount;
//       TotalSize = totalSize;

//        if (TakeCharCount > 1)
//       {
//           ArgumentNullException.ThrowIfNull(charSizeList, nameof(charSizeList));

//           if (charSizeList.Count != TakeCharCount)
//           {
//               throw new ArgumentException($"所记录的字符尺寸信息的数量和所采用的字符数量不匹配。 CharSizeList.Count != TakeCharCount; CharSizeList.Count={charSizeList.Count}；TakeCharCount={TakeCharCount}");
//           }
//       }

//        CharSizeList = charSizeList ?? Array.Empty<LineCharSize>();
//    }

//    public bool CanTake => TakeCharCount > 0;

//    /// <summary>所采用的字符数量</summary>
//    public int TakeCharCount { get;  }

//    /// <summary>总的尺寸</summary>
//    public LineCharSize TotalSize { get;  }

//    /// <summary>各个字符的尺寸。如果采用的字符数量是 1 个时，此属性可以为空，因为字符的尺寸等于 <see cref="TotalSize"/> 尺寸</summary>
//    public IReadOnlyList<LineCharSize> CharSizeList { get; }



//    //public void Deconstruct(out int TakeCharCount, out LineCharSize TotalSize, out IReadOnlyList<LineCharSize>? CharSizeList)
//    //{
//    //    TakeCharCount = this.TakeCharCount;
//    //    TotalSize = this.TotalSize;
//    //    CharSizeList = this.CharSizeList;
//    //}
//}