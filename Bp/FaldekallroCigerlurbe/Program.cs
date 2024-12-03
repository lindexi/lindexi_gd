// See https://aka.ms/new-console-template for more information

using Microsoft.ML.OnnxRuntimeGenAI;

using System.Text;

var folder = @"C:\lindexi\Phi3\directml-int4-awq-block-128\";
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
    prompt = """
             你是一位代码审查者，以下是一些 C# 的代码，你需要审查出代码里面的有哪些地方可以为其编写单元测试。请列举出可以编写单元测试的点
             
             #####
             public partial class TextEditorCore
             {
                 /// <summary>
                 /// 追加一段文本，追加的文本按照段末的样式
                 /// </summary>
                 /// 这是对外调用的，非框架内使用
                 [TextEditorPublicAPI]
                 public void AppendText(string text)
                 {
                     if (string.IsNullOrEmpty(text))
                     {
                         return;
                     }
             
                     DocumentManager.AppendText(new TextRun(text));
                 }
             
                 /// <summary>
                 /// 追加一段文本
                 /// </summary>
                 /// <param name="run"></param>
                 /// 这是对外调用的，非框架内使用
                 [TextEditorPublicAPI]
                 public void AppendRun(IImmutableRun run)
                 {
                     DocumentManager.AppendText(run);
                 }
             
                 /// <summary>
                 /// 在当前的文本上编辑且替换。文本没有选择时，将在当前光标后面加入文本。文本有选择时，替换选择内容为输入内容
                 /// </summary>
                 /// <param name="text"></param>
                 /// <param name="selection">传入空时，将采用 <see cref="CurrentSelection"/> 当前选择范围</param>
                 /// 这是对外调用的，非框架内使用
                 [TextEditorPublicAPI]
                 public void EditAndReplace(string text, Selection? selection = null)
                 {
                     AddLayoutReason("TextEditorCore.EditAndReplace(string text)");
             
                     TextEditorCore textEditor = this;
                     DocumentManager documentManager = textEditor.DocumentManager;
                     // 判断光标是否在文档末尾，且没有选择内容
                     var currentSelection = selection ?? CaretManager.CurrentSelection;
                     var caretOffset = currentSelection.FrontOffset;
                     var isEmptyText = string.IsNullOrEmpty(text);
                     if (currentSelection.IsEmpty && caretOffset.Offset == documentManager.CharCount)
                     {
                         if (!isEmptyText)
                         {
                             // 在末尾，调用追加，性能更好
                             documentManager.AppendText(new TextRun(text));
                         }
                     }
                     else
                     {
                         if (isEmptyText)
                         {
                             documentManager.EditAndReplaceRun(currentSelection, null);
                         }
                         else
                         {
                             var textRun = new TextRun(text);
                             documentManager.EditAndReplaceRun(currentSelection, textRun);
                         }
                     }
                 }
             }
             #####
             
             可以编写单元测试的内容：
             """;

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
