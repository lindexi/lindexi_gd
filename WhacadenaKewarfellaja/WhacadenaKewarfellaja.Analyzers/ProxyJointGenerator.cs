using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Text;

namespace WhacadenaKewarfellaja.Analyzers
{
    [Generator]
    public class ProxyJointGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var mainMethod = context.Compilation.GetEntryPoint(context.CancellationToken);

            string source = $@"
using System;

namespace {mainMethod.ContainingNamespace.ToDisplayString()}
{{
    public static partial class {mainMethod.ContainingType.Name}
    {{
        static void HelloFrom(string name)
        {{
            Console.WriteLine($""Generator says: Hi from '{{name}}'"");
        }}
    }}
}}
";
            context.AddSource("GeneratedSourceTest", source);
        }
    }
}
