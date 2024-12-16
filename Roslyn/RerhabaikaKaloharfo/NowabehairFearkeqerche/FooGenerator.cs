using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices.ComTypes;

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
            var typeNameIncrementalValueProvider = context.CompilationProvider.Select((compilation, token) =>
            {
                foreach (MetadataReference compilationReference in compilation.References)
                {
                    if (compilationReference is PortableExecutableReference portableExecutableReference)
                    {
                        var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(compilationReference) as IAssemblySymbol;
                        if (assemblySymbol?.Name == "DalljukanemDaryawceceegal")
                        {
                            var filePath = portableExecutableReference.FilePath;

                            if (Path.GetDirectoryName(filePath) is var directory && Path.GetFileName(directory) == "ref") 
                            {
                               
                            }

                            //var fileStream = File.OpenRead(filePath);
                            var fileInfo = new FileInfo(filePath);

                            var cSharpDecompiler = new CSharpDecompiler(fileInfo.FullName, new DecompilerSettings()
                            {
                                ThrowOnAssemblyResolveErrors = false
                            });

                            var xamlDecompiler = new XamlDecompiler(fileInfo.FullName, new BamlDecompilerSettings()
                            {
                                ThrowOnAssemblyResolveErrors = false
                            });
                         

                            using (var fileStream = fileInfo.OpenRead())
                            {
                                var metadata = MetadataReaderProvider.FromMetadataStream(fileStream, MetadataStreamOptions.PrefetchMetadata | MetadataStreamOptions.LeaveOpen);

                                var metadataReader = metadata.GetMetadataReader(MetadataReaderOptions.None);

                                var metadataFile = new MetadataFile(MetadataFile.MetadataFileKind.Metadata, fileInfo.FullName, metadata);

                                var peReader = new PEReader(fileStream);

                                var win32ResourceDirectory = peReader.ReadWin32Resources();

                                var peFile = new PEFile(fileInfo.FullName);

                                foreach (var resource in peFile.Resources)
                                {
                                    
                                }



                                var bamlDecompilationResult = xamlDecompiler.Decompile(fileStream);
                            }
                        }
                    }
                }

                var referencedAssemblySymbols = compilation.SourceModule.ReferencedAssemblySymbols;

                foreach (IAssemblySymbol referencedAssemblySymbol in referencedAssemblySymbols)
                {
                    var location = referencedAssemblySymbol.Locations[0];


                    if (referencedAssemblySymbol.Name == "DalljukanemDaryawceceegal")
                    {
                        IAssemblySymbol dalljukanemDaryawceceegalAssembly = referencedAssemblySymbol;


                        using (var metadata = dalljukanemDaryawceceegalAssembly.GetMetadata())
                        {
                            ModuleMetadata module = metadata.GetModules()[0];
                            MetadataReader metadataReader = module.GetMetadataReader();
                            foreach (var assemblyFile in metadataReader.AssemblyFiles)
                            {
                                // 返回 0 个
                            }

                            foreach (var manifestResource in metadataReader.ManifestResources)
                            {
                                // 也是 0 个
                            }

                        }

                        var containingModule = dalljukanemDaryawceceegalAssembly.ContainingModule;
                        var containingModuleLocations = containingModule?.Locations;

                        var moduleSymbol = dalljukanemDaryawceceegalAssembly.Modules.FirstOrDefault();
                        var moduleSymbolLocations = moduleSymbol?.Locations;
                    }
                }

                return referencedAssemblySymbols.Length;
            });

            context.RegisterSourceOutput(typeNameIncrementalValueProvider, (productionContext, provider) =>
            {

            });
        }
    }
}