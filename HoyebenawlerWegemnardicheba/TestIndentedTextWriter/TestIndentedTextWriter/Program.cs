// See https://aka.ms/new-console-template for more information

using System.CodeDom.Compiler;
using System.Text;


var stringBuilder = new StringBuilder();
var stringWriter = new StringWriter(stringBuilder);
var indentedTextWriter = new IndentedTextWriter(stringWriter, " ");
indentedTextWriter.WriteLine("Hello");
indentedTextWriter.WriteLine("Hello");
indentedTextWriter.Indent = 2;

indentedTextWriter.WriteLine("Hello");
indentedTextWriter.WriteLine("Hello");

indentedTextWriter.Indent += 2;
indentedTextWriter.WriteLine("{");

indentedTextWriter.Indent += 4;
indentedTextWriter.WriteLine("Hello");
indentedTextWriter.WriteLine("Hello");

indentedTextWriter.Indent -= 2;
indentedTextWriter.WriteLine("Hello");
indentedTextWriter.Indent -= 2;

indentedTextWriter.WriteLine("}");

indentedTextWriter.Indent -= 2;

Console.WriteLine(stringBuilder.ToString());