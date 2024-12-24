// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using HPPH;
using HPPH.SkiaSharp;
using StableDiffusion.NET;

Debugger.Launch();

var gitFolder = @"D:\lindexi\Taiyi-Stable-Diffusion-1B-Chinese-v0.1";
var modelFile = Path.Join(gitFolder, "Taiyi-Stable-Diffusion-1B-Chinese-v0.1.ckpt");
var vaeFolder = Path.Join(gitFolder, "vae");

using DiffusionModel model = ModelBuilder.StableDiffusion(gitFolder)
    .WithVae(vaeFolder)
    .WithMultithreading()
    .Build();

IImage<ColorRGB> image = model.TextToImage("<prompt>", model.GetDefaultParameter().WithSeed(1234).WithSize(1344, 768));

File.WriteAllBytes("output.png", image.ToPng());