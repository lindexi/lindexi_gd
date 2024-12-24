// See https://aka.ms/new-console-template for more information

using HPPH;
using HPPH.SkiaSharp;
using StableDiffusion.NET;

var gitFolder = @"D:\lindexi\Taiyi-Stable-Diffusion-1B-Chinese-v0.1";
var modelFile = Path.Join(gitFolder, "Taiyi-Stable-Diffusion-1B-Chinese-v0.1.ckpt");
var vaeFolder = Path.Join(gitFolder, "vae");

using DiffusionModel model = ModelBuilder.StableDiffusion(modelFile)
    .WithVae(vaeFolder)
    .WithMultithreading()
    .Build();

IImage<ColorRGB> image = model.TextToImage("<prompt>", model.GetDefaultParameter().WithSeed(1234).WithSize(1344, 768));

File.WriteAllBytes("output.png", image.ToPng());