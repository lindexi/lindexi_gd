// See https://aka.ms/new-console-template for more information
Icu.Wrapper.Init();
// Will output "NFC form of XA\u0308bc is XÄbc"
Console.WriteLine($"NFC form of XA\\u0308bc is {Icu.Normalizer.Normalize("XA\u0308bc",
    Icu.Normalizer.UNormalizationMode.UNORM_NFC)}");
Icu.Wrapper.Cleanup();
