// See https://aka.ms/new-console-template for more information

using HarfBuzzSharp;

using SkiaSharp;

using Buffer = HarfBuzzSharp.Buffer;
using Font = HarfBuzzSharp.Font;

var skFontManager = SKFontManager.Default;
var skTypeface = skFontManager.MatchFamily("微软雅黑");

var asset = skTypeface.OpenStream(out var trueTypeCollectionIndex);
var size = asset.Length;
var memoryBase = asset.GetMemoryBase();

var blob = new Blob(memoryBase, size, MemoryMode.ReadOnly, () => asset.Dispose());
blob.MakeImmutable();

var face = new Face(blob, (uint) trueTypeCollectionIndex);
face.UnitsPerEm = skTypeface.UnitsPerEm;

var fontSize = 20f;

var font = new Font(face);
font.SetFunctionsOpenType();
font.GetScale(out var x, out var y);

float glyphScale = fontSize / x;

var buffer = new Buffer();
//buffer.AddUtf16("林德熙");
buffer.AddUtf32("iw林");
//buffer.GuessSegmentProperties();
buffer.Direction = Direction.LeftToRight;
buffer.Script = Script.Han;

font.Shape(buffer);

var skPaint = new SKPaint();
skPaint.Typeface = skTypeface;
skPaint.TextSize = fontSize;

var length = 0f;
foreach (var glyphPosition in buffer.GlyphPositions)
{
    length += glyphPosition.XOffset * glyphScale + glyphPosition.XAdvance * glyphScale;
}

var measureText = skPaint.MeasureText("iw林");

Console.WriteLine("Hello, World!");
