// See https://aka.ms/new-console-template for more information

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

string code = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrrluujHlcdyqa
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""hellow"");
        }
    }

    class Foo
    {
        public string KiqHns { get; set; }
    }
}";

SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
var rootSyntaxNode = tree.GetRoot();

// 代码着色程序
// 根据 SyntaxTree 解析内容，为 code 代码进行着色
// 着色方法是调用 FillCodeColor 方法完成着色，传入的就是每个代码字符范围和代码类别


Console.WriteLine("Hello, World!");

void FillCodeColor(TextSpan span, ScopeType scope)
{
    // 此方法将会被替换为外部库调用完成着色
}


enum ScopeType
{
    Comment,
    ClassName,
    ClassMember,
    Keyword,
    PlainText,
    String,
    Number,
    Brackets,
    Variable,
}