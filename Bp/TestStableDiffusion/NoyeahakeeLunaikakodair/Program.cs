// See https://aka.ms/new-console-template for more information

using HPPH;
using HPPH.SkiaSharp;
using StableDiffusion.NET;

using DiffusionModel model = ModelBuilder.StableDiffusion(@"<path to model")
    .WithVae(@"<optional path to vae>")
    .WithMultithreading()
    .Build();

IImage<ColorRGB> image = model.TextToImage("<prompt>", model.GetDefaultParameter().WithSeed(1234).WithSize(1344, 768));

File.WriteAllBytes("output.png", image.ToPng());