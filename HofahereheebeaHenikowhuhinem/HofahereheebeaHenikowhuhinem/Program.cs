using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.CSharp;
using Serialize.OpenXml.CodeGen;

namespace HofahereheebeaHenikowhuhinem
{
    class Program
    {
        private static readonly CodeGeneratorOptions Cgo = new CodeGeneratorOptions()
        {
            BracingStyle = "C"
        };

        static void Main(string[] args)
        {
            var sourceFile = new FileInfo(@"C:\Temp\Sample1.xlsx");
            var targetFile = new FileInfo(@"C:\Temp\Sample1.cs");

            using (var source = sourceFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var xlsx = SpreadsheetDocument.Open(source, false))
                {
                    if (xlsx != null)
                    {
                        var codeString = new StringBuilder();
                        var cs = new CSharpCodeProvider();

                        // This will build the CodeCompileUnit object containing all of
                        // the commands that would create the source code to rebuild Sample1.xlsx
                        var code = xlsx.GenerateSourceCode();

                        // This will convert the CodeCompileUnit into C# source code
                        using (var sw = new StringWriter(codeString))
                        {
                            cs.GenerateCodeFromCompileUnit(code, sw, Cgo);
                        }

                        // Save the source code to the target file
                        using (var target = targetFile.Open(FileMode.Create, FileAccess.ReadWrite))
                        {
                            using (var tw = new StreamWriter(target))
                            {
                                tw.Write(codeString.ToString().Trim());
                            }
                        }
                    }
                }
            }

            Console.ReadKey();
        }
    }
}
