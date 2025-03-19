using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DalbawhalelceCujuhurlel.Analyzer;

[Generator(LanguageNames.CSharp)]
public class FooIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<FooInfo1> foo1ValuesProvider = GetProvider();

        IncrementalValuesProvider<FooInfo2> foo2ValuesProvider = foo1ValuesProvider.Select((FooInfo1 info1, CancellationToken token) => new FooInfo2());

        IncrementalValuesProvider<FooInfo3> foo3ValuesProvider = foo1ValuesProvider.Select((FooInfo1 info1, CancellationToken token) => new FooInfo3());

    }

    private IncrementalValuesProvider<FooInfo1> GetProvider()
    {
        throw new NotImplementedException();
    }
}
readonly record struct FooInfo1();
readonly record struct FooInfo2();
readonly record struct FooInfo3();
