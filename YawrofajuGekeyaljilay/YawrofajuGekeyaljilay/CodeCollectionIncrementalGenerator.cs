using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Text;

namespace YawrofajuGekeyaljilay
{
    [Generator(LanguageNames.CSharp)]
    public class CodeCollectionIncrementalGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            string source = @"
using System;

namespace YawrofajuGekeyaljilay
{
    public static partial class Program
    {
        public static void HelloFrom(string name)
        {
            Console.WriteLine($""Says: Hi from '{name}'"");
        }
    }
}
";

            context.RegisterPostInitializationOutput(initializationContext =>
            {
                initializationContext.AddSource("GeneratedSourceTest", source);
            });
        }
    }
}
