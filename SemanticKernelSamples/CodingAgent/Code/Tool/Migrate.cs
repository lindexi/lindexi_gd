#:property TargetFramework=net10.0
#:property ImplicitUsings=enable
#:property Nullable=enable

using System.Text.RegularExpressions;
using System.Xml.Linq;

string root = Directory.GetCurrentDirectory();
string Absolute(string path) => Path.GetFullPath(path.Replace('\\', '/'), root);
string Relative(string directory, string path) => Path.GetRelativePath(directory, path).Replace('\\', '/');

(string Old, string New)[] directories =
[
    ("PrismCodingAgent/Code/PrismCodingAgent.AvaloniaShell", "CodingAgent/Code/App/PrismCodingAgent.AvaloniaShell"),
    ("PrismCodingAgent/Tests/PrismCodingAgent.AvaloniaShell.Tests", "CodingAgent/Code/Test/PrismCodingAgent.AvaloniaShell.Tests"),
    ("PrismCodingAgent/Docs", "CodingAgent/Docs"),
    ("AgentLib", "CodingAgent/Code/Lib/AgentLib"),
    ("DeepSeekSDK", "CodingAgent/Code/Lib/DeepSeekSDK"),
    ("Microsoft.Agents.AI.Extensions", "CodingAgent/Code/Lib/Microsoft.Agents.AI.Extensions"),
    ("Microsoft.Agents.AI.Reasoning", "CodingAgent/Code/Lib/Microsoft.Agents.AI.Reasoning")
];
string[] rootFiles = ["README.md", "README.en.md", "PrismCodingAgent.slnx", "Directory.Build.props"];
var moves = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
foreach (string file in rootFiles)
    moves.Add(Absolute($"PrismCodingAgent/{file}"), Absolute($"CodingAgent/{file}"));

// Enumerating before moving preserves the original base of every relative reference.
var files = SourceFiles(root).ToArray();
foreach (var (oldDirectory, newDirectory) in directories)
{
    string oldPrefix = Absolute(oldDirectory) + Path.DirectorySeparatorChar;
    foreach (string file in files.Where(file => file.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase)))
        moves.Add(file, Path.Combine(Absolute(newDirectory), file[oldPrefix.Length..]));
}

string Map(string path)
{
    if (moves.TryGetValue(path, out string? destination))
        return destination;
    foreach (var (oldDirectory, newDirectory) in directories)
    {
        string oldPath = Absolute(oldDirectory);
        if (path.Equals(oldPath, StringComparison.OrdinalIgnoreCase))
            return Absolute(newDirectory);
        string prefix = oldPath + Path.DirectorySeparatorChar;
        if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return Path.Combine(Absolute(newDirectory), path[prefix.Length..]);
    }
    return path;
}

var xml = files.Where(file => Path.GetExtension(file) is ".csproj" or ".props" or ".targets" or ".slnx")
    .ToDictionary(file => file, file => XDocument.Load(file, LoadOptions.PreserveWhitespace), StringComparer.OrdinalIgnoreCase);
var documents = files.Where(file => Path.GetExtension(file).Equals(".md", StringComparison.OrdinalIgnoreCase))
    .Where(file => moves.ContainsKey(file) || Relative(root, file).StartsWith("ChatRoom/", StringComparison.Ordinal))
    .ToDictionary(file => file, File.ReadAllText, StringComparer.OrdinalIgnoreCase);
var knownPaths = new HashSet<string>(files, StringComparer.OrdinalIgnoreCase);
foreach (string file in files)
{
    for (string? directory = Path.GetDirectoryName(file); directory != root && directory != null; directory = Path.GetDirectoryName(directory))
        knownPaths.Add(directory);
}

string Resolve(string directory, string value) => Path.GetFullPath(value.Replace('\\', '/'), directory);
string RewritePath(string file, string value)
{
    // MSBuild expressions need evaluation, not filesystem path arithmetic.
    if (value.IndexOfAny(['$', '%', '@', ';']) >= 0)
        return value;
    string target = Resolve(Path.GetDirectoryName(file)!, value);
    if (Map(file) == file && Map(target) == target)
        return value;
    string rewritten = Relative(Path.GetDirectoryName(Map(file))!, Map(target));
    return rewritten.Equals(value.Replace('\\', '/'), StringComparison.Ordinal) ? value
        : value.Contains('\\') ? rewritten.Replace('/', '\\') : rewritten;
}

var changedXml = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var (file, document) in xml)
{
    bool solution = Path.GetExtension(file) == ".slnx";
    foreach (XElement element in document.Descendants())
    {
        string name = element.Name.LocalName;
        string[] attributes = solution && name is "Project" or "File" ? ["Path"]
            : name is "ProjectReference" or "Content" or "None" or "Compile" or "EmbeddedResource" or "Resource" or "AvaloniaResource" or "AdditionalFiles"
                ? ["Include", "Update", "Remove"] : [];
        foreach (string attributeName in attributes)
        {
            XAttribute? attribute = element.Attribute(attributeName);
            if (attribute == null)
                continue;
            string rewritten = RewritePath(file, attribute.Value);
            if (rewritten != attribute.Value)
            {
                attribute.Value = rewritten;
                changedXml.Add(file);
            }
        }
        // Link is usually a virtual IDE name; only a known physical target is relocated.
        foreach (XObject link in element.Attributes("Link").Cast<XObject>().Concat(element.Elements("Link")))
        {
            string value = link is XAttribute attribute ? attribute.Value : ((XElement)link).Value;
            if (value.IndexOfAny(['$', '%', '@', ';']) >= 0
                || !knownPaths.Contains(Resolve(Path.GetDirectoryName(file)!, value)))
                continue;
            string rewritten = RewritePath(file, value);
            if (rewritten != value)
            {
                if (link is XAttribute linkAttribute)
                    linkAttribute.Value = rewritten;
                else
                    ((XElement)link).Value = rewritten;
                changedXml.Add(file);
            }
        }
    }
}

string solutionFile = Absolute("PrismCodingAgent/PrismCodingAgent.slnx");
XElement solutionRoot = xml[solutionFile].Root!;
string solutionDirectory = Path.GetDirectoryName(Map(solutionFile))!;
var projects = new HashSet<string>(solutionRoot.Descendants("Project")
    .Select(project => Resolve(solutionDirectory, project.Attribute("Path")!.Value)), StringComparer.OrdinalIgnoreCase);
var projectXml = xml.Where(pair => Path.GetExtension(pair.Key) == ".csproj")
    .ToDictionary(pair => Map(pair.Key), pair => pair.Value, StringComparer.OrdinalIgnoreCase);
var dependencies = new Queue<string>(projects);
while (dependencies.TryDequeue(out string? project))
{
    foreach (XElement reference in projectXml[project].Descendants("ProjectReference"))
    {
        string dependency = Resolve(Path.GetDirectoryName(project)!, reference.Attribute("Include")!.Value);
        if (projects.Add(dependency))
            dependencies.Enqueue(dependency);
    }
}
string[] dependencyTests =
[
    "AgentLib/AgentLib.Tests/AgentLib.Tests.csproj",
    "AgentLib/AgentLib.Coding.Tests/AgentLib.Coding.Tests.csproj",
    "DeepSeekSDK/Code/DeepSeekSDK.Tests/DeepSeekSDK.Tests.csproj"
];
foreach (string test in dependencyTests)
    projects.Add(Map(Absolute(test)));
XElement libraryFolder = solutionRoot.Elements("Folder").Single(folder => (string?)folder.Attribute("Name") == "/Lib/");
var listed = new HashSet<string>(solutionRoot.Descendants("Project")
    .Select(project => Resolve(solutionDirectory, project.Attribute("Path")!.Value)), StringComparer.OrdinalIgnoreCase);
foreach (string project in projects.Order(StringComparer.OrdinalIgnoreCase))
{
    if (listed.Add(project))
        libraryFolder.Add(new XText("\n    "), new XElement("Project", new XAttribute("Path", Relative(solutionDirectory, project))), new XText("\n  "));
}
changedXml.Add(solutionFile);

// Match each original token once: replacements cannot be interpreted as another old path.
var pathToken = new Regex(@"(?<![\w/\\:.\-])(?:[\w.\-]+[/\\])+[\w.\-/\\]*", RegexOptions.CultureInvariant);
var changedDocuments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
foreach (var (file, text) in documents)
{
    string rewritten = pathToken.Replace(text, match =>
    {
        string value = match.Value;
        string repositoryTarget = Absolute(value);
        bool repositoryRelative = knownPaths.Contains(repositoryTarget);
        string target = repositoryRelative ? repositoryTarget : Resolve(Path.GetDirectoryName(file)!, value);
        if (!knownPaths.Contains(target))
            return value;
        string replacement = repositoryRelative ? Relative(root, Map(target))
            : Relative(Path.GetDirectoryName(Map(file))!, Map(target));
        if (value.EndsWith('/') || value.EndsWith('\\'))
            replacement += "/";
        return value.Contains('\\') ? replacement.Replace('/', '\\') : replacement;
    });
    if (rootFiles.Take(2).Any(name => file.Equals(Absolute($"PrismCodingAgent/{name}"), StringComparison.OrdinalIgnoreCase)))
    {
        rewritten = Regex.Replace(rewritten, @"```text\r?\nSemanticKernelSamples/[\s\S]*?```", """
            ```text
            SemanticKernelSamples/
            ├─ ChatRoom/
            └─ CodingAgent/
               ├─ PrismCodingAgent.slnx
               ├─ Directory.Build.props
               ├─ Code/
               │  ├─ App/PrismCodingAgent.AvaloniaShell/
               │  ├─ Test/PrismCodingAgent.AvaloniaShell.Tests/
               │  ├─ Lib/
               │  │  ├─ AgentLib/
               │  │  ├─ DeepSeekSDK/
               │  │  ├─ Microsoft.Agents.AI.Extensions/
               │  │  └─ Microsoft.Agents.AI.Reasoning/
               │  └─ Tool/
               └─ Docs/
            ```
            """);
    }
    if (rewritten != text)
        changedDocuments.Add(file, rewritten);
}

foreach (var (source, destination) in moves)
{
    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
    File.Move(source, destination);
}
foreach (string file in changedXml)
    xml[file].Save(Map(file), SaveOptions.DisableFormatting);
foreach (var (file, text) in changedDocuments)
    File.WriteAllText(Map(file), text);
File.WriteAllText(Absolute("CodingAgent/.gitignore"), File.ReadAllText(Absolute(".gitignore")) + "\nartifacts/\n");

static IEnumerable<string> SourceFiles(string directory)
{
    foreach (string file in Directory.EnumerateFiles(directory))
    {
        string name = Path.GetFileName(file);
        if (!name.EndsWith(".user", StringComparison.OrdinalIgnoreCase)
            && !name.EndsWith(".suo", StringComparison.OrdinalIgnoreCase)
            && !name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
            yield return file;
    }
    foreach (string child in Directory.EnumerateDirectories(directory))
    {
        if (Path.GetFileName(child).ToLowerInvariant() is "bin" or "obj" or "artifacts" or ".vs" or ".git" or ".coding-agent" or ".review-build")
            continue;
        foreach (string file in SourceFiles(child))
            yield return file;
    }
}
