using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices.ComTypes;
using System.Xml.Linq;
using ICSharpCode.BamlDecompiler;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Util;
using Microsoft.CodeAnalysis;

namespace NowabehairFearkeqerche
{
    [Generator(LanguageNames.CSharp)]
    public class FooGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var xamlIncrementalValueProvider = context.CompilationProvider.Select((compilation, token) =>
            {
                var list = new List<XamlRecord>();

                foreach (MetadataReference compilationReference in compilation.References)
                {
                    if (!(compilationReference is PortableExecutableReference portableExecutableReference))
                    {
                        continue;
                    }

                    var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(compilationReference) as IAssemblySymbol;

                    var testDemoProjectName = "DalljukanemDaryawceceegal";

                    if (assemblySymbol?.Name != testDemoProjectName)
                    {
                        continue;
                    }

                    var filePath = portableExecutableReference.FilePath;

                    if (filePath is null)
                    {
                        continue;
                    }

                    if (Path.GetDirectoryName(filePath) is var directory && Path.GetFileName(directory) == "ref")
                    {
                        filePath = Path.Combine(directory, "..", "..", "..", "..", "bin", "Debug",
                            "net9.0-windows", "DalljukanemDaryawceceegal.dll");
                        filePath = Path.GetFullPath(filePath);
                    }

                    var fileInfo = new FileInfo(filePath);

                    var xamlDecompiler = new XamlDecompiler(fileInfo.FullName, new BamlDecompilerSettings()
                    {
                        ThrowOnAssemblyResolveErrors = false
                    });

                    var peFile = new PEFile(fileInfo.FullName);

                    foreach (var resource in peFile.Resources)
                    {
                        if (!(resource.TryOpenStream() is Stream stream))
                        {
                            continue;
                        }

                        var resourcesFile = new ResourcesFile(stream);
                        foreach (var keyValuePair in resourcesFile)
                        {
                            if (keyValuePair.Key.EndsWith(".baml"))
                            {
                                if (keyValuePair.Value is Stream bamlStream)
                                {
                                    var decompilationResult = xamlDecompiler.Decompile(bamlStream);
                                    var xaml = decompilationResult.Xaml;

                                    var xamlRecord = new XamlRecord(xaml, portableExecutableReference,
                                        assemblySymbol);
                                    list.Add(xamlRecord);
                                }
                            }
                        }
                    }
                }

                return list;
            });

            context.RegisterSourceOutput(xamlIncrementalValueProvider, (productionContext, provider) =>
            {

            });
        }
    }

    struct XamlRecord
    {
        public XamlRecord(XDocument xaml, PortableExecutableReference portableExecutableReference,
            IAssemblySymbol assemblySymbol)
        {
            Xaml = xaml;
            PortableExecutableReference = portableExecutableReference;
            AssemblySymbol = assemblySymbol;
        }

        public XDocument Xaml { get; }
        public PortableExecutableReference PortableExecutableReference { get; }
        public IAssemblySymbol AssemblySymbol { get; }
    }
}