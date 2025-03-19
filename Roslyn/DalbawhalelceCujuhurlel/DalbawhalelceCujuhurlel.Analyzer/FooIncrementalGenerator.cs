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

        IncrementalValueProvider<ImmutableArray<FooInfo1>> foo1ArrayValueProvider = foo1ValuesProvider.Collect();

        IncrementalValuesProvider<FooInfo1> backToValuesProvider = foo1ArrayValueProvider.SelectMany((ImmutableArray<FooInfo1> array, CancellationToken token) => array);

        foo1ValuesProvider = backToValuesProvider;
    }

    private IncrementalValuesProvider<FooInfo1> GetProvider()
    {
        throw new NotImplementedException();
    }
}

readonly record struct FooInfo1(int Number);

readonly record struct FooInfo2(int Count);

readonly record struct FooInfo3();