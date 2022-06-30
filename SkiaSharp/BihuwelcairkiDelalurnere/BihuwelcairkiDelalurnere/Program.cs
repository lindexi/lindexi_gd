// See https://aka.ms/new-console-template for more information

using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Skia;

using SkiaSharp;

var skImageInfo = new SKImageInfo(1920, 1080, SKColorType.Bgra8888, SKAlphaType.Opaque, SKColorSpace.CreateSrgb());

var fileName = $"xx.svg";

using var stream = File.OpenWrite(fileName);
using var skCanvas = SKSvgCanvas.Create(new SKRect(0, 0, 100, 100), stream);

var skiaCanvas = new SkiaCanvas();
skiaCanvas.Canvas = skCanvas;

ICanvas canvas = skiaCanvas;

canvas.StrokeSize = 2;
canvas.StrokeColor = Colors.Blue;

canvas.DrawLine(10, 10, 100, 10);

skCanvas.Flush();