// See https://aka.ms/new-console-template for more information

using KebeninegeeWaljelluhi;

var file = @"f:\temp\HalwemcilereKodabeebe.txt";
var outputFile = @"f:\temp\HalwemcilereKodabeebe2.txt";
var file2 = @"f:\temp\WemnoninairLeakejearwair.txt";

foreach (var line in File.ReadAllLines(file))
{
    if (!string.IsNullOrEmpty(line))
    {
        if
        (
            line.Contains("Roslyn", StringComparison.OrdinalIgnoreCase)
            //|| line.Contains("TotalCommander", StringComparison.OrdinalIgnoreCase)
        )
        {
            File.AppendAllText(file2, $"{line}\r\n\r\n");
        }
        else
        {
            File.AppendAllText(outputFile, $"{line}\r\n\r\n");
        }
    }
}

Run<SkiaDrawLine>();
Run<SkiaDrawCircle>();
Run<SkiaDrawRectangle>();
Run<SkiaDrawImageFile>();
Run<SkiaScaleImageFile>();
Run<SkiaDrawRoundRectangle>();
Run<SkiaDrawDissolve>();
Run<SkiaDrawStrokeCaps>();

void Run<T>() where T : SkiaDrawBase, new()
{
    var skiaDraw = new T();
    skiaDraw.Draw();
}