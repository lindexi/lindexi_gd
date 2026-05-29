namespace MiniMaxSdk;

public static class MiniMaxImageResponseFormats
{
    public const string Url = "url";
    public const string Base64 = "base64";

    public static bool IsSupported(string responseFormat) => responseFormat is Url or Base64;
}