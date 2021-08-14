using System;
using System.IO;

#if NETCOREAPP
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
#endif

namespace BawwawnijakeChemlekodoher
{
    class Program
    {
        static void Main(string[] args)
        {
#if NETCOREAPP
            var file = typeof(Program).Assembly.Location;
            file = file.Replace(".dll", ".exe");

            var peReader = new PEReader(File.OpenRead(file));
            if (peReader.HasMetadata)
            {
                var metadata = peReader.GetMetadataReader();
                if (metadata.IsAssembly)
                {
                    foreach (var metadataAssemblyFile in metadata.AssemblyFiles)
                    {
                        var assemblyFile = metadata.GetAssemblyFile(metadataAssemblyFile);
                    }

                    foreach (var metadataAssemblyReference in metadata.AssemblyReferences)
                    {
                        var assemblyReference = metadata.GetAssemblyReference(metadataAssemblyReference);
                        var assemblyName = assemblyReference.GetAssemblyName();
                    }

                    foreach (var metadataCustomAttribute in metadata.CustomAttributes)
                    {
                        var customAttribute = metadata.GetCustomAttribute(metadataCustomAttribute);
                    }
                }

            }

            Foo(file);
#endif
        }

#if NETCOREAPP
        private static void Foo(string file)
        {
            var fx = Path.Combine(file, @"..\..\net45\BawwawnijakeChemlekodoher.exe");
            var peReaderFx = new PEReader(File.OpenRead(fx));
            var peHeadersFx = peReaderFx.PEHeaders;
            var metadataFx = peReaderFx.GetMetadataReader();
            if (metadataFx.IsAssembly)
            {
                foreach (var metadataAssemblyFile in metadataFx.AssemblyFiles)
                {
                    var assemblyFile = metadataFx.GetAssemblyFile(metadataAssemblyFile);
                }

                foreach (var metadataAssemblyReference in metadataFx.AssemblyReferences)
                {
                    var assemblyReference = metadataFx.GetAssemblyReference(metadataAssemblyReference);
                    var assemblyName = assemblyReference.GetAssemblyName();
                }
            }
        }
#endif
    }
}
