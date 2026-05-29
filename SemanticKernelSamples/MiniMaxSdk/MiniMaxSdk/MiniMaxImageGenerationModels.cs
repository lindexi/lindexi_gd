namespace MiniMaxSdk;

public static class MiniMaxImageGenerationModels
{
    public const string Image01 = "image-01";
    public const string Image01Live = "image-01-live";

    public static bool IsSupported(string model) => model is Image01 or Image01Live;
}