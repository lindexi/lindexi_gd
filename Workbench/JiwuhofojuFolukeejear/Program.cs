using Microsoft.Extensions.FileSystemGlobbing;

Matcher matcher = new();
matcher.AddIncludePatterns(new[] { "*.txt", "*.asciidoc", "*.md" });
matcher.AddInclude(@"**\*.db");
matcher.AddInclude(@"f1\*");

string searchDirectory = "../starting-folder/";

var inMemoryDirectoryInfo = new InMemoryDirectoryInfo("Foo",
[
    @"C:\lindexi\1.txt",
    @"C:\lindexi\1.png",
    @"lindexi\2.txt",
    "3.txt",
    @"lindexi\lindexi.db",
    @"lindexi\foo\1.db",
    @"f1\foo\1.db",
    @"f1\2.cd",
]);

PatternMatchingResult patternMatchingResult = matcher.Execute(inMemoryDirectoryInfo);

foreach (var filePatternMatch in patternMatchingResult.Files)
{
    Console.WriteLine($"Stem={filePatternMatch.Stem} Path={filePatternMatch.Path}");
}

Console.WriteLine("Hello, World!");