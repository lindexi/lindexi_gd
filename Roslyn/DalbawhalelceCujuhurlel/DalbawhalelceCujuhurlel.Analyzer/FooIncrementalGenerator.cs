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
        IncrementalValueProvider<FooInfo1> foo1ValuesProvider = GetProvider();

        IncrementalValuesProvider<FooInfo2> foo2ValuesProvider = foo1ValuesProvider.SelectMany
        (
            (FooInfo1 info1, CancellationToken token) =>
            {
                var n = info1.Number;
                var list = new List<FooInfo2>();
                for (int i = 0; i < n; i++)
                {
                    list.Add(new FooInfo2());
                }

                return list;
            }
        );

        IncrementalValuesProvider<FooInfo3> foo3ValuesProvider = foo2ValuesProvider.SelectMany
        (
            (FooInfo2 info2, CancellationToken token) =>
            {
                var list = new List<FooInfo3>();
                for (int i = 0; i < info2.Count; i++)
                {
                    list.Add(new FooInfo3());
                }

                return list;
            }
        );
    }

    private IncrementalValueProvider<FooInfo1> GetProvider()
    {
        throw new NotImplementedException();
    }
}

readonly record struct FooInfo1(int Number);

readonly record struct FooInfo2(int Count);

readonly record struct FooInfo3();