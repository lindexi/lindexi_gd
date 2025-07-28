using Oxage.Wmf;
using Oxage.Wmf.Records;
using SkiaSharp;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SkiaWmfRenderer.Rendering;

class WmfRenderer
{
    public required WmfDocument WmfDocument { get; init; }
    public required int RequestWidth { get; init; }
    public required int RequestHeight { get; init; }

    /// <summary>
    /// 图片压缩的最大宽度
    /// </summary>
    private const int MaxWidth = 3840;

    /// <summary>
    /// 图片压缩的最大高度
    /// </summary>
    private const int MaxHeight = 2160;

    public bool TryRender([NotNullWhen(true)] out SKBitmap? skBitmap)
    {
        skBitmap = null;

        var format = WmfDocument.Format;

        var x = Math.Min(format.Left, format.Right);
        var y = Math.Min(format.Top, format.Bottom);

        var width = Math.Abs(format.Right - format.Left);
        var height = Math.Abs(format.Bottom - format.Top);

        using WmfRenderStatus renderStatus = new()
        {
            CurrentX = x,
            CurrentY = y,
            Width = width,
            Height = height
        };

        var (bitmap, skCanvas) = CreateSKBitmapAndSKCanvas();

        skBitmap = bitmap;

        using SKCanvas canvas = skCanvas;

        try
        {
            var result = TryRenderInner(canvas, renderStatus);

            if (result)
            {
                return true;
            }
            else
            {
                skBitmap.Dispose();
                return false;
            }
        }
        catch (Exception e)
        {
            skBitmap.Dispose();
            Console.WriteLine($"[WmfRenderer] TryRender Fail. {e}");

            return false;
        }
    }

    private (SKBitmap Bitmap, SKCanvas Canvas) CreateSKBitmapAndSKCanvas()
    {
        var format = WmfDocument.Format;
        var offsetX = format.Right > format.Left
            ? -format.Left
            : -format.Right;
        var offsetY = format.Bottom > format.Top
            ? -format.Top
            : -format.Bottom;

        var width = Math.Abs(format.Right - format.Left);
        var height = Math.Abs(format.Bottom - format.Top);

        var renderWidth = width;
        var renderHeight = height;

        var requestWidth = RequestWidth;
        if (requestWidth == 0)
        {
            if (renderWidth > MaxWidth)
            {
                // 约束宽度为最大宽度
                var sx = MaxWidth / (float)renderWidth;
                renderWidth = MaxWidth;
                renderHeight = (int)Math.Round(renderHeight * sx);
            }
            else
            {
                // 保持不动
            }
        }
        else
        {
            var sx = requestWidth / (float)renderWidth;
            renderWidth = requestWidth;
            renderHeight = (int)Math.Round(renderHeight * sx);
        }

        var requestHeight = RequestHeight;
        if (requestHeight == 0)
        {
            if (renderHeight > MaxHeight)
            {
                var sy = MaxHeight / (float)renderHeight;
                renderHeight = MaxHeight;
                renderWidth = (int)Math.Round(renderWidth * sy);
            }
            else
            {
                // 保持不动
            }
        }
        else
        {
            if (requestWidth != 0 && renderHeight <= requestHeight)
            {
                // 如果已经有宽度约束了，且高度没有超过要求的高度，则不需要缩放高度
            }
            else
            {
                var sy = requestHeight / (float)renderHeight;
                renderHeight = requestHeight;
                renderWidth = (int)Math.Round(renderWidth * sy);
            }
        }

        var scaleX = (float)renderWidth / width;
        var scaleY = (float)renderHeight / height;

        var skBitmap = new SKBitmap(renderWidth, renderHeight, SKColorType.Bgra8888, SKAlphaType.Premul);

        SKCanvas canvas = new SKCanvas(skBitmap);

        canvas.Scale(scaleX, scaleY);
        //canvas.Translate(offsetX, offsetY);
        canvas.Save();

        return (skBitmap, canvas);
    }

    private bool TryRenderInner(SKCanvas canvas, WmfRenderStatus renderStatus)
    {
        for (var i = 0; i < WmfDocument.Records.Count; i++)
        {
            var wmfDocumentRecord = WmfDocument.Records[i];
            renderStatus.CurrentRecordsIndex = i;

            var result = RenderRecord(canvas, renderStatus, wmfDocumentRecord);
            if (!result)
            {
                // 如果渲染失败，则返回 false
                return false;
            }
        }

        // 如果包含有文本，且此时有其他编码或带 DX 就表示可以转换，效果不会比 libwmf 更差
        if (renderStatus.IsIncludeText && (renderStatus.IsIncludeOtherEncoding || renderStatus.IsIncludeTextWithDx))
        {
            return true;
        }

        return false;
    }

    private static bool RenderRecord(SKCanvas canvas, WmfRenderStatus renderStatus, IBinaryRecord wmfDocumentRecord)
    {
        if (wmfDocumentRecord is
            // The META_SETBKMODE Record defines the background raster operation mix mode in the playback device context. The background mix mode is the mode for combining pens, text, hatched brushes, and interiors of filled objects with background colors on the output surface.
            WmfSetBkModeRecord setBkModeRecord)
        {
            // RecordFunction (2 bytes): A 16-bit unsigned integer that defines this WMF record type. The lower byte MUST match the lower byte of the RecordType Enumeration (section 2.1.1.1) table value META_SETBKMODE.
            // BkMode (2 bytes): A 16-bit unsigned integer that defines background mix mode. This MUST be one of the values in the MixMode Enumeration (section 2.1.1.20).
        }
        else if (wmfDocumentRecord is WmfSetWindowExtRecord setWindow)
        {
            canvas.Restore();
            canvas.Save();

            if (setWindow.X > 0 && setWindow.Y > 0)
            {
                var scaleX = renderStatus.Width / (float)setWindow.X;
                var scaleY = renderStatus.Height / (float)setWindow.Y;

                canvas.Scale(scaleX, scaleY);
            }
        }
        else if (wmfDocumentRecord is WmfSetTextAlignRecord setTextAlignRecord)
        {
            // RecordFunction (2 bytes): A 16-bit unsigned integer that defines this WMF record type. The lower byte MUST match the lower byte of the RecordType Enumeration (section 2.1.1.1) table value META_SETTEXTALIGN.
            // TextAlignmentMode (2 bytes): A 16-bit unsigned integer that defines text alignment. This value MUST be a combination of one or more TextAlignmentMode Flags (section 2.1.2.3) for text with a horizontal baseline, and VerticalTextAlignmentMode Flags (section 2.1.2.4) for text with a vertical baseline.
        }
        else if (wmfDocumentRecord is WmfCreatePenIndirectRecord createPenIndirectRecord)
        {
            renderStatus.CurrentPenColor = createPenIndirectRecord.Color.ToSKColor();
            renderStatus.CurrentPenThickness =
                Math.Max(createPenIndirectRecord.Width.X, createPenIndirectRecord.Width.Y);

            if (createPenIndirectRecord.Width.X == 0 && createPenIndirectRecord.Width.Y == 0)
            {
                renderStatus.CurrentPenThickness = 1;
            }
        }
        // +		[16]	{== WmfCreateBrushIndirectRecord ==
        // RecordSize: 7 words = 14 bytes
        // RecordType: 0x02fc (RecordType.META_CREATEBRUSHINDIRECT)
        // BrushStyle: BS_SOLID
        // ColorRef: Oxage.Wmf.Primitive.WmfColor
        // BrushHatch: HS_HORIZONTAL
        // }	Oxage.Wmf.IBinaryRecord {Oxage.Wmf.Records.WmfCreateBrushIndirectRecord}
        // 
        else if (wmfDocumentRecord is WmfCreateBrushIndirectRecord createBrushIndirectRecord)
        {
            if (createBrushIndirectRecord.Style == BrushStyle.BS_SOLID)
            {
                renderStatus.CurrentFillColor = createBrushIndirectRecord.Color.ToSKColor();
            }
            else if (createBrushIndirectRecord.Style is BrushStyle.BS_PATTERN or BrushStyle.BS_PATTERN8X8)
            {
                // 可能是贴图，无法处理
                return false;
            }
        }
        // -		[11]	{== WmfMoveToRecord ==
        // RecordSize: 5 words = 10 bytes
        // RecordType: 0x0214 (RecordType.META_MOVETO)
        // X: 1372Y: 865}	Oxage.Wmf.IBinaryRecord {Oxage.Wmf.Records.WmfMoveToRecord}
        // 
        else if (wmfDocumentRecord is WmfMoveToRecord moveToRecord)
        {
            renderStatus.CurrentX = moveToRecord.X;
            renderStatus.CurrentY = moveToRecord.Y;
            renderStatus.LastDxOffset = 0;
        }
        // -		[12]	{== WmfLineToRecord ==
        // RecordSize: 5 words = 10 bytes
        // RecordType: 0x0213 (RecordType.META_LINETO)
        // X: 1840Y: 865}	Oxage.Wmf.IBinaryRecord {Oxage.Wmf.Records.WmfLineToRecord}
        // 
        else if (wmfDocumentRecord is WmfLineToRecord lineToRecord)
        {
            renderStatus.UpdateSkiaStrokeStatus();

            //paint.IsStroke = true;
            //paint.Color = currentPenColor;
            //paint.StrokeWidth = currentPenThickness;
            canvas.DrawLine(renderStatus.CurrentX, renderStatus.CurrentY, lineToRecord.X, lineToRecord.Y,
                renderStatus.Paint);

            renderStatus.CurrentX = lineToRecord.X;
            renderStatus.CurrentY = lineToRecord.Y;
        }
        // -		[20]	{== WmfPolygonRecord ==
        // RecordSize: 26 words = 52 bytes
        // RecordType: 0x0324 (RecordType.META_POLYGON)
        // NumberOfPoints: 11
        // aPoints:
        // 114, 523
        // 200, 475
        // 339, 684
        // 495, 64
        // 1112, 64
        // 1112, 95
        // 518, 95
        // 354, 748
        // 323, 748
        // 167, 516
        // 124, 541
        // }	Oxage.Wmf.IBinaryRecord {Oxage.Wmf.Records.WmfPolygonRecord}
        else if (wmfDocumentRecord is WmfPolygonRecord polygonRecord)
        {
            SKPoint[] skPointArray = polygonRecord.Points.Select(t => t.ToSKPoint()).ToArray();

            using var skPath = new SKPath();
            skPath.AddPoly(skPointArray);

            renderStatus.UpdateSkiaFillStatus();

            canvas.DrawPath(skPath, renderStatus.Paint);
        }
        // +		[16]	{== WmfPolyPolygonRecord ==
        // RecordSize: 5717 words = 11434 bytes
        // RecordType: 0x0538 (RecordType.META_POLYPOLYGON)
        // NumberOfPolygons: 5
        // PointsPerPolygon:
        // 2497
        // 17
        // 17
        // 161
        // 162
        // Points:
        // 7949, 13486
        // 7817, 13485
        // 7677, 13485
        // ...
        else if (wmfDocumentRecord is WmfPolyPolygonRecord polyPolygonRecord)
        {
            // The META_POLYPOLYGON Record paints a series of closed polygons. Each polygon is outlined by using the pen and filled by using the brush and polygon fill mode; these are defined in the playback device context. The polygons drawn by this function can overlap.

            if (polyPolygonRecord.Points.Count < polyPolygonRecord.GetPointsCount())
            {
                return false;
            }

            var currentPointIndex = 0;
            using var skPath = new SKPath();
            foreach (var pointCount in polyPolygonRecord.PointsPerPolygon)
            {
                skPath.Reset();
                var skPointArray = polyPolygonRecord.Points
                    .Skip(currentPointIndex)
                    .Take(pointCount)
                    .Select(t => t.ToSKPoint())
                    .ToArray();
                skPath.AddPoly(skPointArray);

                renderStatus.UpdateSkiaFillStatus();
                canvas.DrawPath(skPath, renderStatus.Paint);
                renderStatus.UpdateSkiaStrokeStatus();
                canvas.DrawPath(skPath, renderStatus.Paint);

                currentPointIndex += pointCount;
            }
        }

        // 文本
        // -		[13]	{== WmfSetTextColorRecord ==
        // RecordSize: 5 words = 10 bytes
        // RecordType: 0x0209 (RecordType.META_SETTEXTCOLOR)
        // ColorRef: Color [A=255, R=0, G=0, B=0]
        // }	Oxage.Wmf.IBinaryRecord {Oxage.Wmf.Records.WmfSetTextColorRecord}
        // 
        else if (wmfDocumentRecord is WmfSetTextColorRecord setTextColorRecord)
        {
            renderStatus.CurrentTextColor = setTextColorRecord.Color.ToSKColor();
        }
        // -		[15]	{== WmfCreateFontIndirectRecord ==
        // RecordSize: 28 words = 56 bytes
        // RecordType: 0x02fb (RecordType.META_CREATEFONTINDIRECT)
        // Height: -636
        // Width: 0
        // Escapement: 0
        // Orientation: 0
        // Weight: 400
        // Italic: False
        // Underline: False
        // StrikeOut: False
        // CharSet: 0x0000 (CharacterSet.ANSI_CHARSET)
        // OutPrecision: 0x00 (OutPrecision.OUT_DEFAULT_PRECIS)
        // ClipPrecision: 0x02 (ClipPrecision.CLIP_STROKE_PRECIS)
        // Quality: 0x00 (FontQuality.DEFAULT_QUALITY)
        // Pitch: 0x00 (PitchFont.DEFAULT_PITCH)
        // Family: 0x00 (FamilyFont.FF_DONTCARE)
        // Facename: Times New Roman	Oxage.Wmf.IBinaryRecord {Oxage.Wmf.Records.WmfCreateFontIndirectRecord}
        // 
        else if (wmfDocumentRecord is WmfCreateFontIndirectRecord createFontIndirectRecord)
        {
            // 	"Times New Roman\0è±ñwñ±ñw @ów³\u0011fÁ"
            renderStatus.CurrentFontName = createFontIndirectRecord.FaceName.ToString();
            renderStatus.CurrentCharacterSet = createFontIndirectRecord.CharSet;

            // Width (2 bytes): A 16-bit signed integer that defines the average width, in logical units, of characters in the font. If Width is 0x0000, the aspect ratio of the device SHOULD be matched against the digitization aspect ratio of the available fonts to find the closest match, determined by the absolute value of the difference.
            var fontWidth = createFontIndirectRecord.Width;

            // Height (2 bytes): A 16-bit signed integer that specifies the height, in logical units, of the font's character cell. The character height is computed as the character cell height minus the internal leading. The font mapper SHOULD interpret the height as follows.
            // value < 0x0000 The font mapper SHOULD transform this value into device units and match its absolute value against the character height of available fonts.
            // 0x0000 A default height value MUST be used when creating a physical font.
            // 0x0000 < value The font mapper SHOULD transform this value into device units and match it against the cell height of available fonts.
            // For all height comparisons, the font mapper SHOULD find the largest physical font that does not exceed the requested size.<40>
            // <40> Section 2.2.1.2: All Windows versions: mapping the logical font size to the available physical fonts occurs the first time the logical font needs to be used in a drawing operation.
            // For the MM_TEXT mapping mode, the following formula can be used to compute the height of a font with a specified point size.
            // 	Height = -MulDiv(PointSize, GetDeviceCaps(hDC, LOGPIXELSY), 72);
            // 
            var fontHeight = createFontIndirectRecord.Height;

            fontWidth = Math.Abs(fontWidth);
            fontHeight = Math.Abs(fontHeight);

            renderStatus.CurrentFontSize = Math.Max(fontWidth, fontHeight);

            renderStatus.IsItalic = createFontIndirectRecord.Italic;
            renderStatus.FontWeight = createFontIndirectRecord.Weight;
        }
        else if (wmfDocumentRecord is WmfExtTextoutRecord extTextoutRecord)
        {
            renderStatus.IsIncludeText = true;

            // 是否包含了其他编码
            var isIncludeOtherEncoding = renderStatus.CurrentCharacterSet != CharacterSet.DEFAULT_CHARSET;
            renderStatus.IsIncludeOtherEncoding |=
                isIncludeOtherEncoding;

            var isIncludeTextWithDx = renderStatus.LastDxOffset > 0;
            renderStatus.IsIncludeTextWithDx |=
                isIncludeTextWithDx;

            var text = extTextoutRecord.GetText(renderStatus.CurrentEncoding);

            var currentXOffset = renderStatus.CurrentX + extTextoutRecord.X + renderStatus.LastDxOffset;

            renderStatus.UpdateSkiaTextStatus();

            if (extTextoutRecord.Dx is null)
            {
                canvas.DrawText(text, currentXOffset, renderStatus.CurrentY + extTextoutRecord.Y, renderStatus.SKFont,
                    renderStatus.Paint);
            }
            else
            {
                // 关于字间距的规则： 
                // 1. 如果两个 META_EXTTEXTOUT 相邻，中间没有 MoveTo 之类
                // 则第二个 META_EXTTEXTOUT 将需要使用前一个 META_EXTTEXTOUT 的 dx 末项
                // 2. 可选的 dx 是存放在字符串末尾的可选项，从文档 2.3.3.5 上可见 dx 是顶格写的，这就意味着这个值是一定对齐整数倍的。由于 dx 是放在数据末尾，可通过减法算出 dx 长度，即数据总长度减去所有已知字段的长度加上字符串长度，剩余的就是 dx 长度。如果计算返回的 dx 长度是奇数，则首个 byte 是需要跳过的，如此就能确保在 16bit 下的 wmf 格式里面，读取的 dx 是从整数倍开始读取
                // 参考 https://learn.microsoft.com/zh-cn/windows/win32/api/wingdi/nf-wingdi-exttextoutw
                // 测试 17 项

                if (extTextoutRecord.Dx.Length != text.Length)
                {
                    return false;
                }

                for (var textIndex = 0; textIndex < text.Length; textIndex++)
                {
                    canvas.DrawText(text[textIndex].ToString(), currentXOffset,
                        renderStatus.CurrentY + extTextoutRecord.Y, renderStatus.SKFont,
                        renderStatus.Paint);

                    currentXOffset += extTextoutRecord.Dx[textIndex];
                }

                renderStatus.LastDxOffset = extTextoutRecord.Dx[^1];
            }
        }
        else if (wmfDocumentRecord is WmfUnknownRecord unknownRecord)
        {
            switch (unknownRecord.RecordType)
            {
                case RecordType.META_EXTTEXTOUT:
                {
                    renderStatus.IsIncludeText = true;

                    // 关于字间距的规则： 
                    // 1. 如果两个 META_EXTTEXTOUT 相邻，中间没有 MoveTo 之类
                    // 则第二个 META_EXTTEXTOUT 将需要使用前一个 META_EXTTEXTOUT 的 dx 末项
                    // 2. 可选的 dx 是存放在字符串末尾的可选项，从文档 2.3.3.5 上可见 dx 是顶格写的，这就意味着这个值是一定对齐整数倍的。由于 dx 是放在数据末尾，可通过减法算出 dx 长度，即数据总长度减去所有已知字段的长度加上字符串长度，剩余的就是 dx 长度。如果计算返回的 dx 长度是奇数，则首个 byte 是需要跳过的，如此就能确保在 16bit 下的 wmf 格式里面，读取的 dx 是从整数倍开始读取
                    // 参考 https://learn.microsoft.com/zh-cn/windows/win32/api/wingdi/nf-wingdi-exttextoutw
                    // 测试 17 项
                        var memoryStream = new MemoryStream(unknownRecord.Data);
                    var binaryReader = new BinaryReader(memoryStream);
                    var ty = binaryReader.ReadUInt16();
                    var tx = binaryReader.ReadUInt16();
                    var stringLength = binaryReader.ReadUInt16();
                    var fwOpts = (ExtTextOutOptions)binaryReader.ReadUInt16();
                    // Rectangle (8 bytes): An optional 8-byte Rect Object (section 2.2.2.18).) When either ETO_CLIPPED, ETO_OPAQUE, or both are specified, the rectangle defines the dimensions, in logical coordinates, used for clipping, opaquing, or both. When neither ETO_CLIPPED nor ETO_OPAQUE is specified, the coordinates in Rectangle are ignored.
                    var st = 8; /*2byte of ty tx stringLength fwOpts*/
                    if (fwOpts is ExtTextOutOptions.ETO_CLIPPED or ExtTextOutOptions.ETO_OPAQUE)
                    {
                        // 此时才有 Rectangle 的值
                        binaryReader.ReadBytes(8);
                        st += 8;
                    }

                    st += stringLength;

                    // String (variable): A variable-length string that specifies the text to be drawn. The string does not need to be null-terminated, because StringLength specifies the length of the string. If the length is odd, an extra byte is placed after it so that the following member (optional Dx) is aligned on a 16-bit boundary. The string will be decoded based on the font object currently selected into the playback device context. If a font matching the font object’s specification is not found, the decoding is undefined. If a matching font is found that matches the charset specified in the font object, the string should be decoded with the codepages in the following table.
                    var stringBuffer = binaryReader.ReadBytes(stringLength);
                    var text = renderStatus.CurrentEncoding.GetString(stringBuffer);

                    // 是否包含了其他编码
                    var isIncludeOtherEncoding = renderStatus.CurrentCharacterSet != CharacterSet.DEFAULT_CHARSET;
                    renderStatus.IsIncludeOtherEncoding |=
                        isIncludeOtherEncoding;

                    // Dx (variable): An optional array of 16-bit signed integers that indicate the distance between origins of adjacent character cells. For example, Dx[i] logical units separate the origins of character cell i and character cell i + 1. If this field is present, there MUST be the same number of values as there are characters in the string.
                    //Debug.Assert(st == unknownRecord.RecordSize);
                    var dxLength = unknownRecord.Data.Length - st;

                    renderStatus.UpdateSkiaTextStatus();

                    var currentXOffset = renderStatus.CurrentX + tx + renderStatus.LastDxOffset;

                    var isIncludeTextWithDx = renderStatus.LastDxOffset > 0;
                    renderStatus.IsIncludeTextWithDx |=
                        isIncludeTextWithDx;

                    if (dxLength == 0)
                    {
                        canvas.DrawText(text, currentXOffset, renderStatus.CurrentY + ty, renderStatus.SKFont,
                            renderStatus.Paint);
                    }
                    else
                    {
                        // 如果这里计算出来不是偶数，则首个需要跳过。这是经过测试验证的。~~但没有相关说明内容。且跳过的 byte 是有内容的~~ String (variable): If the length is odd, an extra byte is placed after it so that the following member (optional Dx) is aligned on a 16-bit boundary.
                        // 如果字符串的长度是奇数，则在字符串后面放置一个额外的字节，以便下一个成员（可选的 Dx）对齐到 16 位边界。
                        if (dxLength > ((dxLength / sizeof(UInt16)) * sizeof(UInt16)))
                        {
                            // 读取掉这个额外的字节，以便 Dx 对齐到 16 位边界
                            var r = binaryReader.ReadByte();
                            _ = r;
                        }

                        UInt16[] dxArray = new UInt16[dxLength / sizeof(UInt16)];
                        for (var t = 0; t < dxArray.Length; t++)
                        {
                            dxArray[t] = binaryReader.ReadUInt16();
                        }

                        if (dxArray.Length != text.Length)
                        {
                            return false;
                        }

                        for (var textIndex = 0; textIndex < text.Length; textIndex++)
                        {
                            canvas.DrawText(text[textIndex].ToString(), currentXOffset,
                                renderStatus.CurrentY + ty, renderStatus.SKFont,
                                renderStatus.Paint);

                            currentXOffset += dxArray[textIndex];
                        }

                        renderStatus.LastDxOffset = dxArray[^1];
                    }

                    break;
                }
            }
        }

        return true;
    }
}