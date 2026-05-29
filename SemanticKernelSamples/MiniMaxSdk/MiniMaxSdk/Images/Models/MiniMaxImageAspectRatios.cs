namespace MiniMaxSdk;

public static class MiniMaxImageAspectRatios
{
    public const string Square = "1:1";
    public const string Landscape16By9 = "16:9";
    public const string Standard4By3 = "4:3";
    public const string Standard3By2 = "3:2";
    public const string Portrait2By3 = "2:3";
    public const string Portrait3By4 = "3:4";
    public const string Portrait9By16 = "9:16";
    public const string Ultrawide21By9 = "21:9";

    public static bool IsSupported(string aspectRatio) => aspectRatio is Square or Landscape16By9 or Standard4By3 or Standard3By2 or Portrait2By3 or Portrait3By4 or Portrait9By16 or Ultrawide21By9;
}