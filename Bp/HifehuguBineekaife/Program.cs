// See https://aka.ms/new-console-template for more information

using Microsoft.ML.OnnxRuntime;

var modelFilePath = "model.onnx";

if (args.Length > 0)
{
    modelFilePath = args[0];
}

using SessionOptions options = new SessionOptions();
options.LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_INFO;

// Load the model
using var session = new InferenceSession(modelFilePath, options);

Console.WriteLine("Hello, World!");
