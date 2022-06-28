// See https://aka.ms/new-console-template for more information

using KebeninegeeWaljelluhi;

Run<SkiaDrawLine>();
Run<SkiaDrawCircle>();
Run<SkiaDrawRectangle>();
Run<SkiaDrawImageFile>();

void Run<T>() where T : SkiaDrawBase, new()
{
    var skiaDraw = new T();
    skiaDraw.Draw();
}