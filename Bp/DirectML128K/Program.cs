// See https://aka.ms/new-console-template for more information

using Microsoft.ML.OnnxRuntimeGenAI;

using System.Text;

var folder = @"C:\lindexi\Phi3\Phi-3-mini-128k-instruct-onnx\directml\directml-int4-awq-block-128";
if (!Directory.Exists(folder))
{
    folder = Path.GetFullPath(".");
}

using var model = new Model(folder);
using var tokenizer = new Tokenizer(model);

var systemPrompt = "You are a helpfull assistant";

for(var i = 0; i < int.MaxValue; i++)
{
    Console.WriteLine("请输入聊天内容");

    var text = Console.ReadLine();

    var userPrompt = text;

    var prompt = $@"<|system|>{systemPrompt}<|end|><|user|>{userPrompt}<|end|><|assistant|>";

    var generatorParams = new GeneratorParams(model);

    var sequences = tokenizer.Encode(prompt);

    generatorParams.SetSearchOption("max_length", 1024);
    generatorParams.SetInputSequences(sequences);
    generatorParams.TryGraphCaptureWithMaxBatchSize(1);

    using var tokenizerStream = tokenizer.CreateStream();
    using var generator = new Generator(model, generatorParams);
    StringBuilder stringBuilder = new();

    while (!generator.IsDone())
    {
        generator.ComputeLogits();
        generator.GenerateNextToken();

        // 这里的 tokenSequences 就是在输入的 sequences 后面添加 Token 内容
        var tokenSequences = generator.GetSequence(0);

        // 每次只会添加一个 Token 值
        // 需要调用 tokenizerStream 的解码将其转为人类可读的文本
        // 由于不是每一个 Token 都对应一个词，因此需要根据 tokenizerStream 压入进行转换，而不是直接调用 tokenizer.Decode 方法，或者调用 tokenizer.Decode 方法，每次都全部转换

        // 当前全部的文本
        var allText = tokenizer.Decode(tokenSequences);

        // 取最后一个进行解码为文本
        var decodeText = tokenizerStream.Decode(tokenSequences[^1]);
        // 有些时候这个 decodeText 是一个空文本，有些时候是一个单词
        // 空文本的可能原因是需要多个 token 才能组成一个单词
        // 在 tokenizerStream 底层已经处理了这样的情况，会在需要多个 Token 才能组成一个单词的情况下，自动合并，在多个 Token 中间的 Token 都返回空字符串，最后一个 Token 才返回组成的单词
        if (!string.IsNullOrEmpty(decodeText))
        {
            stringBuilder.Append(decodeText);
        }
        Console.Write(decodeText);
    }

    Console.WriteLine("完成对话");
}

Console.WriteLine("Hello, World!");
