// See https://aka.ms/new-console-template for more information

var ints = new List<int>();

var code =
    $$"""
      var ints = new List<int>();
      
      {{F(() => {

          if (ints.Count == 0) {
              yield return "// No integers";
        }

           })}}
      
      Console.WriteLine(string.Join(", ", ints));
      """;

Console.WriteLine("Hello, World!");

string F(Func<IEnumerable<string>> func)
{
    var result = func();
    return string.Join("", result);
}