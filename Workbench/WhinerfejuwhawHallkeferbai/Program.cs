// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

// 加上引用程序集，防止找不到引用
var referenceAssemblyPaths = new[]
{
    typeof(object).Assembly.Location,
    typeof(Console).Assembly.Location,
};

var adhocWorkspace = new AdhocWorkspace(MefHostServices.DefaultHost);
Solution solution = adhocWorkspace.CurrentSolution;

var csharpCompilationOptions = new CSharpCompilationOptions
(
    OutputKind.DynamicallyLinkedLibrary, // 输出类型 dll 类型
    usings: new[] { "System" }, // 引用的命名空间
    allowUnsafe: true, // 允许不安全代码
    sourceReferenceResolver: new SourceFileResolver(new[] { Environment.CurrentDirectory },
        Environment.CurrentDirectory)
);

var project = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(),
    name: "Lindexi",
    assemblyName: "Lindexi",
    language: csharpCompilationOptions.Language,
    metadataReferences: referenceAssemblyPaths.Select(t => MetadataReference.CreateFromFile(t)));

var documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(project.Id), name: "LindexiCode", sourceCodeKind: SourceCodeKind.Script);

solution = solution.AddProject(project);
solution = solution.AddDocument(documentInfo);

Document document = solution.GetDocument(documentInfo.Id)!;

// 模拟输入的代码。预期输入 "Cons" 能够得到 Console 补全
var text = "Cons";

Document textDocument = document.WithText(SourceText.From(text));
CompletionService completionService = CompletionService.GetService(document)!;
Debug.Assert(completionService != null);

// 获取补全列表
CompletionList completionList = await completionService.GetCompletionsAsync(textDocument, caretPosition: text.Length);

foreach (var completionItem in completionList.ItemsList
             .OrderBy(item => item.DisplayText.StartsWith(text) ? 0 : 1)
             .ThenByDescending(item => item.Rules.MatchPriority)
             .ThenBy(item => item.SortText))
{
    if (!completionItem.Properties.TryGetKey("DescriptionProperty", out var description))
    {
        description = string.Empty;
    }

    Console.WriteLine($"""
                       DisplayText:{completionItem.DisplayText}
                       SortText:{completionItem.SortText}
                       FilterText:{completionItem.FilterText}
                       MatchPriority:{completionItem.Rules.MatchPriority}
                       Description:{description}
                       
                       """);
}

Thread.Sleep(Timeout.Infinite);