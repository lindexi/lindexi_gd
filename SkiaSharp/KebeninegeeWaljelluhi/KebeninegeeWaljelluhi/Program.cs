// See https://aka.ms/new-console-template for more information

using KebeninegeeWaljelluhi;

Run<SkiaDrawLine>();
Run<SkiaDrawCircle>();

void Run<T>() where T : SkiaDrawBase, new()
{
    var skiaDraw = new T();
    skiaDraw.Draw();
}