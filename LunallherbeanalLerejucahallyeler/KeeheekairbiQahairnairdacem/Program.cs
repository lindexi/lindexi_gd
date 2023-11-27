// See https://aka.ms/new-console-template for more information

using System.Text;

var stringBuilder = new StringBuilder();
stringBuilder.AppendLine("""
                         using System.Windows;
                         
                         namespace LunallherbeanalLerejucahallyeler;
                         public partial class MainWindow
                         {
                         """);

for (int i = 0; i < ushort.MaxValue + 1; i++)
{
    var code = $$"""
                 public static readonly DependencyProperty Foo{{i}}Property = DependencyProperty.Register(
                     nameof(Foo{{i}}), typeof(string), typeof(MainWindow), new PropertyMetadata(default(string)));

                 public string Foo{{i}}
                 {
                     get { return (string)GetValue(Foo1Property); }
                     set { SetValue(Foo1Property, value); }
                 }
                 """;
    stringBuilder.AppendLine(code);
}

stringBuilder.AppendLine("}");

var file = "MainWindow.cs";
File.WriteAllText(file,stringBuilder.ToString());

