// See https://aka.ms/new-console-template for more information

using System.Text.RegularExpressions;

var code =
    """
    <Color x:Key="ApplicationBackgroundColorLight">#FFFAFAFA</Color>
       <SolidColorBrush x:Key="ApplicationBackgroundColorLightBrush" Color="{StaticResource ApplicationBackgroundColorLight}" />
    
       <Color x:Key="ApplicationBackgroundColorDark">#FF202020</Color>
       <SolidColorBrush x:Key="ApplicationBackgroundColorDarkBrush" Color="{StaticResource ApplicationBackgroundColorDark}" />
    
       <Color x:Key="ControlStrongFillColorLight">#B3FFFFFF</Color>
       <SolidColorBrush x:Key="ControlStrongFillColorLightBrush" Color="{StaticResource ControlStrongFillColorLight}" />
    
       <Color x:Key="ControlStrongFillColorDark">#72000000</Color>
       <SolidColorBrush x:Key="ControlStrongFillColorDarkBrush" Color="{StaticResource ControlStrongFillColorDark}" />
    """;

var stringReader = new StringReader(code);
string line;
while ((line = stringReader.ReadLine()) != null)
{
    if (!string.IsNullOrEmpty(line) && line.Contains("<SolidColorBrush ", StringComparison.Ordinal))
    {
        var match = Regex.Match(line,@"Key=""(\w+)"" Color=""{StaticResource (\w+)}""");
        if (match.Success)
        {
            var brush = match.Groups[1].Value;
            var color = match.Groups[2].Value;

            if (brush != (color + "Brush"))
            {

            }
        }
    }
}

Console.WriteLine("Hello, World!");
