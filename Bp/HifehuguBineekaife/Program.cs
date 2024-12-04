// See https://aka.ms/new-console-template for more information

using System.Collections.ObjectModel;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntimeGenAI;

var modelFilePath = "model.onnx";

var folder =
    @"C:\lindexi\Phi3\Phi-3-medium-128k-instruct-onnx-cuda\cuda-int4-rtn-block-32";

using var model = new Model(folder);
using var tokenizer = new Tokenizer(model);

if (args.Length > 0)
{
    modelFilePath = args[0];
}

using SessionOptions options = new SessionOptions();
options.LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_INFO;

// Load the model
using var session = new InferenceSession(modelFilePath, options);

session.Run(new ReadOnlyCollection<NamedOnnxValue>(new List<NamedOnnxValue>()
{
}));

Console.WriteLine("Hello, World!");
