using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Text;

namespace WpfXamlSourceGenerator
{
    [Generator(LanguageNames.CSharp)]
    internal class XamlSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
        }
    }
}
