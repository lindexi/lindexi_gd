namespace LightTextEditorPlus.Core.Editing;

public static class TextFeaturesExtension
{
    public static TextFeatures EnableFeatures(this TextFeatures currentFeatures, TextFeatures features)
    {
        return currentFeatures | features;
    }

    public static TextFeatures DisableFeatures(this TextFeatures currentFeatures, TextFeatures features)
    {
        return currentFeatures & ~features;
    }

    public static bool IsFeaturesEnable(this TextFeatures currentFeatures, TextFeatures features)
    {
        return (currentFeatures & features) == features;
    }
}