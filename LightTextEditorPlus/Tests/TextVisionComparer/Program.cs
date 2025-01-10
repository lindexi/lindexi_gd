// See https://aka.ms/new-console-template for more information

using TextVisionComparer;

VisionComparer visionComparer = new VisionComparer();
var result = visionComparer.Compare(new FileInfo("4a15iio0.50f.png"), new FileInfo("v31zohcv.ri3.png"));
result.IsSimilar();

Console.WriteLine("Hello, World!");
