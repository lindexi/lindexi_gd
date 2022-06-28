// See https://aka.ms/new-console-template for more information

using KebeninegeeWaljelluhi;

Run<SkiaDrawLine>();
Run<SkiaDrawCircle>();
Run<SkiaDrawRectangle>();
Run<SkiaDrawImageFile>();
Run<SkiaScaleImageFile>();
Run<SkiaDrawRoundRectangle>();
Run<SkiaDrawDissolve>();

void Run<T>() where T : SkiaDrawBase, new()
{
    var skiaDraw = new T();
    skiaDraw.Draw();
}