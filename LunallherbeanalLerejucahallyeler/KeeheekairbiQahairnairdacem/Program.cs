// See https://aka.ms/new-console-template for more information

using System.Text;

var stringBuilder = new StringBuilder();
stringBuilder.AppendLine("""
                         using System.Windows;
                         
                         namespace LunallherbeanalLerejucahallyeler;
                         """);

for (int i = 0; i < 10; i++)
{
    stringBuilder.AppendLine($$"""
                             public partial class Type{{i}}
                             {
                             """);

    for (int j = 0; j < 7000; j++)
    {
        var code = $$"""
                     public static readonly DependencyProperty Foo{{j}}Property = DependencyProperty.Register(
                         nameof(Foo{{j}}), typeof(string), typeof(Type{{i}}), new PropertyMetadata(default(string)));

                     public string Foo{{j}}
                     {
                         get { return (string)GetValue(Foo{{j}}Property); }
                         set { SetValue(Foo{{j}}Property, value); }
                     }
                     """;
        stringBuilder.AppendLine(code);
    }

    stringBuilder.AppendLine("}");

}



var file = "MainWindow.cs";
File.WriteAllText(file,stringBuilder.ToString());

