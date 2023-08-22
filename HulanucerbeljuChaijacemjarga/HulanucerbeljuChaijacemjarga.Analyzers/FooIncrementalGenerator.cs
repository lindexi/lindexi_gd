using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HulanucerbeljuChaijacemjarga.Analyzers
{
    [Generator(LanguageNames.CSharp)]
    public class FooIncrementalGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            Debugger.Launch();

            var argumentTypeProvider = context.SyntaxProvider.CreateSyntaxProvider(
                (node, _) =>
                {
                    return node.IsKind(SyntaxKind.MethodDeclaration);
                }, (syntaxContext, _) =>
                {
                    var symbol = syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node);
                    var methodSymbol = (IMethodSymbol)symbol;

                    if (methodSymbol.Name == "Fx")
                    {
                        var methodDeclarationSyntax = (MethodDeclarationSyntax) syntaxContext.Node;

                        var parameterListSyntax = methodDeclarationSyntax.ParameterList;
                        var argumentTypeSyntax = parameterListSyntax.Parameters[0].Type;
                        return (ITypeSymbol) syntaxContext.SemanticModel.GetSymbolInfo(argumentTypeSyntax).Symbol;
                    }

                    return null;
                }).Where(t => t != null).Collect();

            var typeNameIncrementalValueProvider = context.CompilationProvider.Combine(argumentTypeProvider).Select((tuple, token) =>
                {
                    if (tuple.Right.Length == 0)
                    {
                        return new List<INamedTypeSymbol>();
                    }

                    var compilation = tuple.Left;
                    var typeSymbol = tuple.Right[0];

                    // 获取所在的程序集
                    var containingAssembly = typeSymbol.ContainingAssembly;

                    // 获取到所有引用程序集
                    var referencedAssemblySymbols = compilation.SourceModule.ReferencedAssemblySymbols;

                    var list = new List<INamedTypeSymbol>();

                    foreach (var referencedAssemblySymbol in referencedAssemblySymbols)
                    {
                        if (SymbolEqualityComparer.Default.Equals(referencedAssemblySymbol, containingAssembly))
                        {
                        }

                        // 判断程序集的引用关系
                        if (referencedAssemblySymbol.Modules.Any(t =>
                                t.ReferencedAssemblySymbols.Any(x =>
                                    SymbolEqualityComparer.Default.Equals(x, containingAssembly))))
                        {
                            // 这是引用包含的程序集
                            // 获取所有的类型
                            // 这里 ToList 只是为了方便调试
                            var allTypeSymbol = GetAllTypeSymbol(referencedAssemblySymbol.GlobalNamespace);
                            foreach (var type in allTypeSymbol)
                            {
                                Debug.WriteLine(type.Name);

                                if (IsInherit(type, typeSymbol))
                                {
                                    list.Add(type);
                                }
                            }
                        }
                        else
                        {
                            // 其他的引用程序集，在这里就忽略
                        }
                    }

                    return list;
                });

            context.RegisterSourceOutput(typeNameIncrementalValueProvider, (productionContext, list) => { });
        }

        private static IEnumerable<INamedTypeSymbol> GetAllTypeSymbol(INamespaceSymbol namespaceSymbol)
        {
            var typeMemberList = namespaceSymbol.GetTypeMembers();

            foreach (var typeSymbol in typeMemberList)
            {
                yield return typeSymbol;
            }

            foreach (var namespaceMember in namespaceSymbol.GetNamespaceMembers())
            {
                foreach (var typeSymbol in GetAllTypeSymbol(namespaceMember))
                {
                    yield return typeSymbol;
                }
            }
        }

        /// <summary>
        /// 判断类型继承关系
        /// </summary>
        /// <param name="currentType">当前的类型</param>
        /// <param name="requiredType">需要继承的类型</param>
        /// <returns></returns>
        private static bool IsInherit(ITypeSymbol currentType, ITypeSymbol requiredType)
        {
            var baseType = currentType.BaseType;
            while (baseType != null)
            {
                if (SymbolEqualityComparer.Default.Equals(baseType, requiredType))
                {
                    // 如果基类型是的话
                    return true;
                }

                // 否则继续找基类型
                baseType = baseType.BaseType;
            }

            foreach (var currentInheritInterfaceType in currentType.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(currentInheritInterfaceType, requiredType))
                {
                    // 如果继承的类型是的话
                    return true;
                }
            }

            return false;
        }
    }
}