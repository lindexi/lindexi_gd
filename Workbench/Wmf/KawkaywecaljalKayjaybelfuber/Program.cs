// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using Oxage.Wmf;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using Oxage.Wmf.Records;
using SkiaSharp;

var file = @"C:\lindexi\wmf公式\image17.wmf";
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var image = Image.FromFile(file);
var imageWidth = image.Width;
var imageHeight = image.Height;
var imagePhysicalDimension = image.PhysicalDimension;

using var fileStream = File.OpenRead(file);
var wmfDocument = new WmfDocument();
wmfDocument.Load(fileStream);


var format = wmfDocument.Format;
Console.WriteLine(format.Dump());

// Left: 61528
// Top: 62158
// Right: 4008
// Bottom: 3378
// Unit: 1000
// Checksum: 21749

var x = Math.Min(format.Left, format.Right);
var y = Math.Min(format.Top, format.Bottom);

var width = Math.Abs(format.Right - format.Left);
var height = Math.Abs(format.Bottom - format.Top);

var inchUnit = format.Unit;
var pixelWidth = (double)width / inchUnit * 96;
var pixelHeight = (double)height / inchUnit * 96;

var skBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
SKCanvas canvas = new SKCanvas(skBitmap);

var currentPenColor = SKColors.Empty;
var currentPenThickness = 0;

float currentX = x;
float currentY = y;

var currentTextColor = SKColors.Black;

using var paint = new SKPaint();
paint.IsAntialias = true;

using var skFont = new SKFont();
float currentFontSize = 0;

string? currentFontName = null;
CharacterSet currentCharacterSet = CharacterSet.ANSI_CHARSET;
Encoding currentEncoding = Encoding.GetEncoding(1252);

Encoding CharacterSetToEncoding(CharacterSet characterSet)
{
    var codePageId = characterSet switch
    {
        CharacterSet.ANSI_CHARSET
            // DEFAULT_CHARSET: Specifies a character set based on the current system locale; for example, when the system locale is United States English, the default character set is ANSI_CHARSET.
            or CharacterSet.DEFAULT_CHARSET => 1252,
        CharacterSet.OEM_CHARSET => 437,
        CharacterSet.SHIFTJIS_CHARSET => 932,
        CharacterSet.HANGUL_CHARSET => 949,
        CharacterSet.JOHAB_CHARSET => 1361,
        CharacterSet.GB2312_CHARSET => 936,
        CharacterSet.CHINESEBIG5_CHARSET => 950,
        CharacterSet.HEBREW_CHARSET => 1255,
        CharacterSet.ARABIC_CHARSET => 1256,
        CharacterSet.GREEK_CHARSET => 1253,
        CharacterSet.TURKISH_CHARSET => 1254,
        CharacterSet.BALTIC_CHARSET => 1257,
        CharacterSet.EASTEUROPE_CHARSET => 1250,
        CharacterSet.RUSSIAN_CHARSET => 1251,
        CharacterSet.THAI_CHARSET => 874,
        CharacterSet.VIETNAMESE_CHARSET => 1258,
        CharacterSet.SYMBOL_CHARSET => 42, // Symbol font is not a code page, but 42 is often used for Symbol font
        _ => 1252,
    };

    return Encoding.GetEncoding(codePageId);
}

float lastDxOffset = 0;

bool isItalic = false;
int fontWeight = 400;

for (var i = 0; i < wmfDocument.Records.Count; i++)
{
    var wmfDocumentRecord = wmfDocument.Records[i];
    switch (wmfDocumentRecord)
    {
        // The META_SETBKMODE Record defines the background raster operation mix mode in the playback device context. The background mix mode is the mode for combining pens, text, hatched brushes, and interiors of filled objects with background colors on the output surface.
        case WmfSetBkModeRecord setBkModeRecord:
        {
            // RecordFunction (2 bytes): A 16-bit unsigned integer that defines this WMF record type. The lower byte MUST match the lower byte of the RecordType Enumeration (section 2.1.1.1) table value META_SETBKMODE.
            // BkMode (2 bytes): A 16-bit unsigned integer that defines background mix mode. This MUST be one of the values in the MixMode Enumeration (section 2.1.1.20).

            break;
        }
        case WmfSetTextAlignRecord setTextAlignRecord:
        {
            // RecordFunction (2 bytes): A 16-bit unsigned integer that defines this WMF record type. The lower byte MUST match the lower byte of the RecordType Enumeration (section 2.1.1.1) table value META_SETTEXTALIGN.
            // TextAlignmentMode (2 bytes): A 16-bit unsigned integer that defines text alignment. This value MUST be a combination of one or more TextAlignmentMode Flags (section 2.1.2.3) for text with a horizontal baseline, and VerticalTextAlignmentMode Flags (section 2.1.2.4) for text with a vertical baseline.

            break;
        }
        case WmfCreatePenIndirectRecord createPenIndirectRecord:
        {
            Color color = createPenIndirectRecord.Color;
            currentPenColor = new SKColor(color.R, color.G, color.B, color.A);
            currentPenThickness = Math.Max(createPenIndirectRecord.Width.X, createPenIndirectRecord.Width.Y);
            break;
        }
        // -		[11]	{== WmfMoveToRecord ==
        // RecordSize: 5 words = 10 bytes
        // RecordType: 0x0214 (RecordType.META_MOVETO)
        // X: 1372Y: 865}	Oxage.Wmf.IBinaryRecord {Oxage.Wmf.Records.WmfMoveToRecord}
        // 
        case WmfMoveToRecord moveToRecord:
        {
            currentX = moveToRecord.X;
            currentY = moveToRecord.Y;
            lastDxOffset = 0;
            break;
        }
        // -		[12]	{== WmfLineToRecord ==
        // RecordSize: 5 words = 10 bytes
        // RecordType: 0x0213 (RecordType.META_LINETO)
        // X: 1840Y: 865}	Oxage.Wmf.IBinaryRecord {Oxage.Wmf.Records.WmfLineToRecord}
        // 
        case WmfLineToRecord lineToRecord:
        {
            paint.IsStroke = true;
            paint.Color = currentPenColor;
            paint.StrokeWidth = currentPenThickness;

            canvas.DrawLine(currentX, currentY, lineToRecord.X, lineToRecord.Y, paint);

            break;
        }
        // -		[13]	{== WmfSetTextColorRecord ==
        // RecordSize: 5 words = 10 bytes
        // RecordType: 0x0209 (RecordType.META_SETTEXTCOLOR)
        // ColorRef: Color [A=255, R=0, G=0, B=0]
        // }	Oxage.Wmf.IBinaryRecord {Oxage.Wmf.Records.WmfSetTextColorRecord}
        // 
        case WmfSetTextColorRecord setTextColorRecord:
        {
            currentTextColor = ToSKColor(setTextColorRecord.Color);

            break;
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
        case WmfCreateFontIndirectRecord createFontIndirectRecord:
        {
            // 	"Times New Roman\0è±ñwñ±ñw @ów³\u0011fÁ"
            currentFontName = createFontIndirectRecord.FaceName.ToString();
            currentCharacterSet = createFontIndirectRecord.CharSet;
            currentEncoding = CharacterSetToEncoding(currentCharacterSet);

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

            currentFontSize = Math.Max(fontWidth, fontHeight);

            isItalic = createFontIndirectRecord.Italic;
            fontWeight = createFontIndirectRecord.Weight;

            break;
        }
        case WmfUnknownRecord unknownRecord:
        {
            switch (unknownRecord.RecordType)
            {
                case RecordType.META_EXTTEXTOUT:
                {
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
                    var st = 8; /*2byte ofr ty tx stringLength fwOpts*/
                    if (fwOpts is ExtTextOutOptions.ETO_CLIPPED or ExtTextOutOptions.ETO_OPAQUE)
                    {
                        // 此时才有 Rectangle 的值
                        binaryReader.ReadBytes(8);
                        st += 8;
                    }

                    st += stringLength;

                    // String (variable): A variable-length string that specifies the text to be drawn. The string does not need to be null-terminated, because StringLength specifies the length of the string. If the length is odd, an extra byte is placed after it so that the following member (optional Dx) is aligned on a 16-bit boundary. The string will be decoded based on the font object currently selected into the playback device context. If a font matching the font object’s specification is not found, the decoding is undefined. If a matching font is found that matches the charset specified in the font object, the string should be decoded with the codepages in the following table.
                    var stringBuffer = binaryReader.ReadBytes(stringLength);
                    var text = currentEncoding.GetString(stringBuffer);

                    // Dx (variable): An optional array of 16-bit signed integers that indicate the distance between origins of adjacent character cells. For example, Dx[i] logical units separate the origins of character cell i and character cell i + 1. If this field is present, there MUST be the same number of values as there are characters in the string.
                    //Debug.Assert(st == unknownRecord.RecordSize);
                    var dxLength = unknownRecord.Data.Length - st;

                    skFont.Size = currentFontSize;
                    skFont.Typeface = SKTypeface.FromFamilyName(currentFontName, (SKFontStyleWeight)fontWeight,
                        SKFontStyleWidth.Normal, isItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);

                    paint.Style = SKPaintStyle.Fill;
                    paint.Color = currentTextColor;

                    var currentXOffset = currentX + tx + lastDxOffset;

                    if (dxLength == 0)
                    {
                        canvas.DrawText(text, currentXOffset, currentY + ty, skFont, paint);
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
                            continue;
                        }

                        for (var textIndex = 0; textIndex < text.Length; textIndex++)
                        {
                            canvas.DrawText(text[textIndex].ToString(), currentXOffset, currentY + ty, skFont, paint);

                            currentXOffset += dxArray[textIndex];
                        }

                        lastDxOffset = dxArray[^1];
                    }

                    break;
                }
            }

            break;
        }
    }
}

var outputFile = "1.png";
using (var outputStream = File.OpenWrite(outputFile))
{
    skBitmap.Encode(outputStream, SKEncodedImageFormat.Png, 100);
}

Console.WriteLine("Hello, World!");

SKColor ToSKColor(Color color)
{
    return new SKColor(color.R, color.G, color.B, color.A);
}