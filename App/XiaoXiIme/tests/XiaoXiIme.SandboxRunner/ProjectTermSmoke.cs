using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeCore;
using XiaoXiIme.ImeHost;
using XiaoXiIme.ImeInterop;
using XiaoXiIme.ImeIpc;
using XiaoXiIme.ImeModule;

namespace XiaoXiIme.SandboxRunner;

internal static class ProjectTermSmoke
{
    private const string ToAsciiPathName = "toascii";
    private const string ProcessKeyPathName = "process-key";

    internal static int Run(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var stdoutPath = Path.Combine(outputDirectory, "stdout.txt");
        var stderrPath = Path.Combine(outputDirectory, "stderr.txt");
        var resultPath = Path.Combine(outputDirectory, "result.json");
        var startedAt = DateTimeOffset.UtcNow;
        var queries = new List<SmokeQuery>();
        var exitCode = 0;
        var stdout = string.Empty;
        var stderr = string.Empty;

        try
        {
            var root = Path.Combine(Path.GetTempPath(), "XiaoXiIme.SandboxRunner", Guid.NewGuid().ToString("N"));
            var hostRoot = Path.Combine(root, "host");
            var fullPinyinPath = Path.Combine(hostRoot, DictionaryPackageLocations.FullPinyinPackageDirectoryName);
            var xiaohePath = Path.Combine(hostRoot, DictionaryPackageLocations.XiaoheDoublePinyinPackageRelativePath);
            var entries = XiaoXiImeProjectTerms.Parse();
            DictionaryPackageCompiler.Compile(entries, fullPinyinPath);
            DictionaryPackageCompiler.Compile(
                entries,
                xiaohePath,
                new DictionaryPackageParameters { InputScheme = DictionaryPackageLocations.XiaoheDoublePinyinInputScheme });

            var fullPinyin = DictionaryPackageLoader.Load(fullPinyinPath);
            var xiaohe = DictionaryPackageLoader.Load(xiaohePath);
            AssertText(fullPinyin, "xiaoxiaimuyi", "XiaoXiIme", queries);
            AssertText(xiaohe, "xnxiaimuyi", "XiaoXiIme", queries);
            AssertEmpty(xiaohe, "xiaoxiaimuyi", queries);
            AssertText(xiaohe, "xx", "小希", queries);
            AssertText(xiaohe, "aimuyi", "IME", queries);
            AssertComposition(fullPinyin, "xiaoxiaimuyi", "XiaoXiIme", queries);
            AssertCommit(fullPinyin, "xiaoxiaimuyi", "XiaoXiIme", queries);
            AssertComposition(xiaohe, "xnxiaimuyi", "XiaoXiIme", queries);
            AssertCommit(xiaohe, "xnxiaimuyi", "XiaoXiIme", queries);
            AssertHost(hostRoot, DictionaryPackageLocations.FullPinyinInputScheme, "xiaoxiaimuyi", "XiaoXiIme", queries);
            AssertHost(hostRoot, DictionaryPackageLocations.XiaoheDoublePinyinInputScheme, "xnxiaimuyi", "XiaoXiIme", queries);
            AssertBridge(hostRoot, DictionaryPackageLocations.FullPinyinInputScheme, "xiaoxiaimuyi", "XiaoXiIme", queries);
            AssertBridge(hostRoot, DictionaryPackageLocations.XiaoheDoublePinyinInputScheme, "xnxiaimuyi", "XiaoXiIme", queries);
            AssertSetComposition(hostRoot, DictionaryPackageLocations.FullPinyinInputScheme, "xiaoxiaimuyi", "XiaoXiIme", queries);
            AssertSetComposition(hostRoot, DictionaryPackageLocations.XiaoheDoublePinyinInputScheme, "xnxiaimuyi", "XiaoXiIme", queries);
            AssertConversionList(hostRoot, DictionaryPackageLocations.FullPinyinInputScheme, "xiaoxiaimuyi", "XiaoXiIme", queries);
            AssertConversionList(hostRoot, DictionaryPackageLocations.XiaoheDoublePinyinInputScheme, "xnxiaimuyi", "XiaoXiIme", queries);
            AssertReverseConversionList(hostRoot, DictionaryPackageLocations.FullPinyinInputScheme, queries);
            AssertReverseConversionList(hostRoot, DictionaryPackageLocations.XiaoheDoublePinyinInputScheme, queries);
            AssertReverseLength(hostRoot, DictionaryPackageLocations.FullPinyinInputScheme, queries);
            AssertReverseLength(hostRoot, DictionaryPackageLocations.XiaoheDoublePinyinInputScheme, queries);
            AssertImeEscape(hostRoot, DictionaryPackageLocations.FullPinyinInputScheme, "xiaoxiaimuyi", queries);
            AssertRegisterWord(hostRoot, DictionaryPackageLocations.FullPinyinInputScheme, "xiaoxiaimuyi", queries);
            AssertImeConfigureRegisterWord(hostRoot, DictionaryPackageLocations.FullPinyinInputScheme, "xiaoxiaimuyi", queries);
            AssertToAsciiEx(hostRoot, DictionaryPackageLocations.FullPinyinInputScheme, "xiaoxiaimuyi", "XiaoXiIme", queries);
            AssertToAsciiEx(hostRoot, DictionaryPackageLocations.XiaoheDoublePinyinInputScheme, "xnxiaimuyi", "XiaoXiIme", queries);
            AssertProcessKey(hostRoot, DictionaryPackageLocations.FullPinyinInputScheme, "xiaoxiaimuyi", "XiaoXiIme", queries);
            AssertProcessKey(hostRoot, DictionaryPackageLocations.XiaoheDoublePinyinInputScheme, "xnxiaimuyi", "XiaoXiIme", queries);
            AssertImeSelectCancel(hostRoot, DictionaryPackageLocations.FullPinyinInputScheme, "xiaoxiaimuyi", queries);
            AssertImeSetActiveContextCancel(hostRoot, DictionaryPackageLocations.FullPinyinInputScheme, "xiaoxiaimuyi", queries);
            AssertNotifyImeCancel(hostRoot, DictionaryPackageLocations.FullPinyinInputScheme, "xiaoxiaimuyi", queries);
            AssertNotifyImeComplete(hostRoot, DictionaryPackageLocations.XiaoheDoublePinyinInputScheme, "xnxiaimuyi", "XiaoXiIme", queries);
            AssertNotifyImeSelectCandidate(hostRoot, DictionaryPackageLocations.FullPinyinInputScheme, "xiaoxiaimuyi", "XiaoXiIme", queries);
            AssertNotifyImeRevert(hostRoot, DictionaryPackageLocations.FullPinyinInputScheme, "xiaoxiaimuyi", queries);
            AssertIndependentHimcSessions(hostRoot, queries);
            AssertIndependentIpcSessions(hostRoot, queries);
            AssertImeDestroyResetsSessions(hostRoot, queries);
            RequireQueries(
                queries,
                "process-key:fullPinyin:xiaoxiaimuyi",
                "process-key-commit:fullPinyin:xiaoxiaimuyi",
                "process-key:xiaoheDoublePinyin:xnxiaimuyi",
                "process-key-commit:xiaoheDoublePinyin:xnxiaimuyi",
                "set-composition:fullPinyin:xiaoxiaimuyi",
                "set-composition-clear:fullPinyin:xiaoxiaimuyi",
                "set-composition:xiaoheDoublePinyin:xnxiaimuyi",
                "set-composition-clear:xiaoheDoublePinyin:xnxiaimuyi",
                "conversion-list:fullPinyin:xiaoxiaimuyi",
                "conversion-list:xiaoheDoublePinyin:xnxiaimuyi",
                "reverse-conversion-list:fullPinyin:XiaoXiIme",
                "reverse-conversion-list:xiaoheDoublePinyin:XiaoXiIme",
                "reverse-length:fullPinyin:XiaoXiIme",
                "reverse-length:xiaoheDoublePinyin:XiaoXiIme",
                "ime-escape:fullPinyin:xiaoxiaimuyi",
                "register-word:fullPinyin:xiaoxiaimuyi",
                "register-word-enum:fullPinyin:xiaoxiaimuyi",
                "unregister-word:fullPinyin:xiaoxiaimuyi",
                "ime-configure:fullPinyin:xiaoxiaimuyi",
                "ime-configure-enum:fullPinyin:xiaoxiaimuyi",
                "ime-select-cancel:fullPinyin:xiaoxiaimuyi",
                "ime-set-active-cancel:fullPinyin:xiaoxiaimuyi",
                "notify-cancel:fullPinyin:xiaoxiaimuyi",
                "notify-complete:xiaoheDoublePinyin:xnxiaimuyi",
                "notify-select:fullPinyin:xiaoxiaimuyi",
                "notify-revert:fullPinyin:xiaoxiaimuyi",
                "independent-himc:fullPinyin:xiaoxiaimuyi",
                "independent-himc-commit:fullPinyin:aimuyi",
                "independent-ipc:fullPinyin:xiaoxiaimuyi",
                "independent-ipc:fullPinyin:aimuyi",
                "independent-ipc-commit:fullPinyin:aimuyi",
                "independent-ipc-fallback:fullPinyin:aimuyi",
                "independent-ipc-reset:fullPinyin:xiaoxiaimuyi",
                "ime-destroy:fullPinyin:xiaoxiaimuyi");
            stdout = "Project-term smoke passed. process-key-himc=verified set-composition=verified conversion-list=verified reverse-conversion-list=verified reverse-length=verified ime-escape=verified register-word=verified ime-configure=verified ime-select-notify=verified notify-select=verified notify-revert=verified independent-himc=verified independent-ipc=verified session-reset=verified";
        }
        catch (Exception exception)
        {
            exitCode = 1;
            stderr = exception.ToString();
        }

        File.WriteAllText(stdoutPath, stdout);
        File.WriteAllText(stderrPath, stderr);
        File.WriteAllText(
            resultPath,
            System.Text.Json.JsonSerializer.Serialize(
                new
                {
                    operation = "project-term-smoke",
                    startedAt,
                    endedAt = DateTimeOffset.UtcNow,
                    exitCode,
                    queries,
                    stdoutPath = "stdout.txt",
                    stderrPath = "stderr.txt",
                },
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                }));
        return exitCode;
    }

    private static void AssertText(IImeDictionary dictionary, string input, string expectedText, List<SmokeQuery> queries)
    {
        var candidates = dictionary.Query(new ImeDictionaryQuery(input));
        var actual = candidates.Count == 1 ? candidates[0].Text : string.Join(',', candidates.Select(candidate => candidate.Text));
        queries.Add(new SmokeQuery(input, expectedText, actual, candidates.Count));
        if (candidates.Count != 1 || !string.Equals(candidates[0].Text, expectedText, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected '{expectedText}' for '{input}', got [{actual}].");
        }
    }

    private static void AssertEmpty(IImeDictionary dictionary, string input, List<SmokeQuery> queries)
    {
        var candidates = dictionary.Query(new ImeDictionaryQuery(input));
        queries.Add(new SmokeQuery(input, string.Empty, string.Join(',', candidates.Select(candidate => candidate.Text)), candidates.Count));
        if (candidates.Count != 0)
        {
            throw new InvalidOperationException($"Expected no candidates for '{input}'.");
        }
    }

    private static void AssertComposition(IImeDictionary dictionary, string input, string expectedText, List<SmokeQuery> queries)
    {
        var context = new ImeContext(dictionary);
        ImeProcessResult result = default!;
        foreach (var character in input)
        {
            result = context.ProcessKey(ImeKey.FromCharacter(character));
        }

        var actual = result.Snapshot.Candidates.Count == 0
            ? string.Empty
            : result.Snapshot.Candidates[0].Text;
        queries.Add(new SmokeQuery($"compose:{input}", expectedText, actual, result.Snapshot.Candidates.Count));
        if (!string.Equals(actual, expectedText, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected composition '{expectedText}' for '{input}', got '{actual}'.");
        }
    }

    private static void AssertCommit(IImeDictionary dictionary, string input, string expectedText, List<SmokeQuery> queries)
    {
        var context = new ImeContext(dictionary);
        foreach (var character in input)
        {
            context.ProcessKey(ImeKey.FromCharacter(character));
        }

        var result = context.ProcessKey(new ImeKey(ImeKeyKind.Space));
        queries.Add(new SmokeQuery($"commit:{input}", expectedText, result.CommitText ?? string.Empty, result.Snapshot.Candidates.Count));
        if (!string.Equals(result.CommitText, expectedText, StringComparison.Ordinal) || result.Snapshot.IsComposing)
        {
            throw new InvalidOperationException($"Expected commit '{expectedText}' for '{input}', got '{result.CommitText}'.");
        }
    }

    private static void AssertHost(string hostRoot, string inputScheme, string input, string expectedText, List<SmokeQuery> queries)
    {
        using var host = new ImeHostService(
            inputScheme: inputScheme,
            hostBaseDirectory: hostRoot,
            userDictionaryPath: Path.Combine(hostRoot, $"user-{inputScheme}.json"));
        ImeProcessResult result = default!;
        foreach (var character in input)
        {
            result = host.ProcessKeyAsync(ImeKey.FromCharacter(character)).GetAwaiter().GetResult();
        }

        var actual = result.Snapshot.Candidates.Count == 0
            ? string.Empty
            : result.Snapshot.Candidates[0].Text;
        queries.Add(new SmokeQuery($"host:{inputScheme}:{input}", expectedText, actual, result.Snapshot.Candidates.Count));
        if (!string.Equals(actual, expectedText, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected host '{expectedText}' for '{inputScheme}:{input}', got '{actual}'.");
        }

        var commit = host.ProcessKeyAsync(new ImeKey(ImeKeyKind.Space)).GetAwaiter().GetResult();
        queries.Add(new SmokeQuery($"host-commit:{inputScheme}:{input}", expectedText, commit.CommitText ?? string.Empty, commit.Snapshot.Candidates.Count));
        if (!string.Equals(commit.CommitText, expectedText, StringComparison.Ordinal) || commit.Snapshot.IsComposing)
        {
            throw new InvalidOperationException($"Expected host commit '{expectedText}' for '{inputScheme}:{input}', got '{commit.CommitText}'.");
        }

        var status = host.GetHostStatusAsync().GetAwaiter().GetResult();
        if (status.IsUsingFallbackDictionary)
        {
            throw new InvalidOperationException($"Host used fallback dictionary for '{inputScheme}'.");
        }

        if (!string.Equals(status.DictionaryInputScheme, inputScheme, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected host scheme '{inputScheme}', got '{status.DictionaryInputScheme}'.");
        }
    }

    private static void AssertBridge(string hostRoot, string inputScheme, string input, string expectedText, List<SmokeQuery> queries)
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Smoke_{Guid.NewGuid():N}");
        using var host = new ImeHostService(
            options,
            inputScheme: inputScheme,
            hostBaseDirectory: hostRoot,
            userDictionaryPath: Path.Combine(hostRoot, $"bridge-user-{inputScheme}.json"));
        using var bridge = new ImeHostBridge(options);
        host.Start();

        ImeProcessResult result = default!;
        foreach (var character in input)
        {
            result = bridge.ProcessKey(ImeKey.FromCharacter(character));
        }

        var actual = result.Snapshot.Candidates.Count == 0
            ? string.Empty
            : result.Snapshot.Candidates[0].Text;
        queries.Add(new SmokeQuery($"bridge:{inputScheme}:{input}", expectedText, actual, result.Snapshot.Candidates.Count));
        if (!string.Equals(actual, expectedText, StringComparison.Ordinal) || bridge.IsUsingFallback)
        {
            throw new InvalidOperationException($"Expected bridge '{expectedText}' for '{inputScheme}:{input}', got '{actual}'.");
        }

        var commit = bridge.ProcessKey(new ImeKey(ImeKeyKind.Space));
        queries.Add(new SmokeQuery($"bridge-commit:{inputScheme}:{input}", expectedText, commit.CommitText ?? string.Empty, commit.Snapshot.Candidates.Count));
        if (!string.Equals(commit.CommitText, expectedText, StringComparison.Ordinal) || commit.Snapshot.IsComposing || bridge.IsUsingFallback)
        {
            throw new InvalidOperationException($"Expected bridge commit '{expectedText}' for '{inputScheme}:{input}', got '{commit.CommitText}'.");
        }
    }

    private static unsafe void AssertSetComposition(string hostRoot, string inputScheme, string input, string expectedText, List<SmokeQuery> queries)
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Smoke_setcomp_{Guid.NewGuid():N}");
        using var host = new ImeHostService(
            options,
            inputScheme: inputScheme,
            hostBaseDirectory: hostRoot,
            userDictionaryPath: Path.Combine(hostRoot, $"setcomp-user-{inputScheme}.json"));
        ImeModuleRuntime.SetBridgeForTesting(new ImeHostBridge(options));
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());

        try
        {
            host.Start();
            int result;
            fixed (char* compositionPointer = input)
            {
                result = ImeExports.ImeSetCompositionStringManaged(
                    himc.Handle,
                    ImeConstants.ScsSetStr,
                    compositionPointer,
                    (uint)(input.Length * sizeof(char)),
                    null,
                    0);
            }

            var lastResult = ImeModuleRuntime.GetLastProcessResultForTesting();
            var actual = lastResult.Snapshot.Candidates.Count == 0
                ? string.Empty
                : lastResult.Snapshot.Candidates[0].Text;
            queries.Add(new SmokeQuery($"set-composition:{inputScheme}:{input}", expectedText, actual, result));
            if (result != 1
                || lastResult.CommitText is not null
                || !string.Equals(actual, expectedText, StringComparison.Ordinal)
                || !string.Equals(himc.ReadCompositionText(), input, StringComparison.Ordinal)
                || himc.CompositionString->ResultStrLength != 0)
            {
                throw new InvalidOperationException($"Expected set-composition '{expectedText}' for '{inputScheme}:{input}', got '{actual}'.");
            }

            int cleared;
            fixed (char* emptyPointer = string.Empty)
            {
                cleared = ImeExports.ImeSetCompositionStringManaged(
                    himc.Handle,
                    ImeConstants.ScsSetStr,
                    emptyPointer,
                    0,
                    null,
                    0);
            }

            var clearedResult = ImeModuleRuntime.GetLastProcessResultForTesting();
            queries.Add(new SmokeQuery($"set-composition-clear:{inputScheme}:{input}", string.Empty, clearedResult.Snapshot.Composition.Reading, cleared));
            if (cleared != 1
                || clearedResult.Snapshot.IsComposing
                || himc.CompositionString->CompStrLength != 0
                || himc.CompositionString->ResultStrLength != 0)
            {
                throw new InvalidOperationException($"Expected set-composition to clear '{inputScheme}:{input}' without writing a result string.");
            }
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    private static unsafe void AssertConversionList(string hostRoot, string inputScheme, string input, string expectedText, List<SmokeQuery> queries)
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Smoke_gcl_{Guid.NewGuid():N}");
        using var host = new ImeHostService(
            options,
            inputScheme: inputScheme,
            hostBaseDirectory: hostRoot,
            userDictionaryPath: Path.Combine(hostRoot, $"gcl-user-{inputScheme}.json"));
        ImeModuleRuntime.SetBridgeForTesting(new ImeHostBridge(options));
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());
        const string currentComposition = "n";
        var buffer = new byte[256];

        try
        {
            host.Start();
            fixed (char* compositionPointer = currentComposition)
            {
                var setResult = ImeExports.ImeSetCompositionStringManaged(
                    himc.Handle,
                    ImeConstants.ScsSetStr,
                    compositionPointer,
                    (uint)(currentComposition.Length * sizeof(char)),
                    null,
                    0);
                if (setResult != 1 || !string.Equals(himc.ReadCompositionText(), currentComposition, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Expected conversion-list setup composition '{currentComposition}'.");
                }
            }

            uint written;
            string actual;
            fixed (char* sourcePointer = input)
            fixed (byte* destination = buffer)
            {
                written = ImeExports.ImeConversionListManaged(
                    himc.Handle,
                    sourcePointer,
                    destination,
                    (uint)buffer.Length,
                    ImeConstants.GclConversion);
                var candidateList = (CandidateList*)destination;
                actual = written == 0 || candidateList->Count == 0
                    ? string.Empty
                    : new string((char*)(destination + candidateList->Offset[0]));
            }

            queries.Add(new SmokeQuery($"conversion-list:{inputScheme}:{input}", expectedText, actual, (int)written));
            if (written == 0
                || !string.Equals(actual, expectedText, StringComparison.Ordinal)
                || !string.Equals(himc.ReadCompositionText(), currentComposition, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Expected conversion-list '{expectedText}' for '{inputScheme}:{input}', got '{actual}'.");
            }
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    private static unsafe void AssertReverseConversionList(string hostRoot, string inputScheme, List<SmokeQuery> queries)
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Smoke_rgcl_{Guid.NewGuid():N}");
        using var host = new ImeHostService(
            options,
            inputScheme: inputScheme,
            hostBaseDirectory: hostRoot,
            userDictionaryPath: Path.Combine(hostRoot, $"rgcl-user-{inputScheme}.json"));
        ImeModuleRuntime.SetBridgeForTesting(new ImeHostBridge(options));
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());
        const string currentComposition = "n";
        const string source = "XiaoXiIme";
        const string expectedReading = "xiao xi ai mu yi";
        var buffer = new byte[256];

        try
        {
            host.Start();
            fixed (char* compositionPointer = currentComposition)
            {
                var setResult = ImeExports.ImeSetCompositionStringManaged(
                    himc.Handle,
                    ImeConstants.ScsSetStr,
                    compositionPointer,
                    (uint)(currentComposition.Length * sizeof(char)),
                    null,
                    0);
                if (setResult != 1 || !string.Equals(himc.ReadCompositionText(), currentComposition, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Expected reverse-conversion-list setup composition '{currentComposition}'.");
                }
            }

            uint written;
            string actual;
            fixed (char* sourcePointer = source)
            fixed (byte* destination = buffer)
            {
                written = ImeExports.ImeConversionListManaged(
                    himc.Handle,
                    sourcePointer,
                    destination,
                    (uint)buffer.Length,
                    ImeConstants.GclReverseConversion);
                var candidateList = (CandidateList*)destination;
                actual = written == 0 || candidateList->Count == 0
                    ? string.Empty
                    : new string((char*)(destination + candidateList->Offset[0]));
            }

            queries.Add(new SmokeQuery($"reverse-conversion-list:{inputScheme}:{source}", expectedReading, actual, (int)written));
            if (written == 0
                || !string.Equals(actual, expectedReading, StringComparison.Ordinal)
                || !string.Equals(himc.ReadCompositionText(), currentComposition, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Expected reverse-conversion-list '{expectedReading}' for '{inputScheme}:{source}', got '{actual}'.");
            }
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    private static unsafe void AssertReverseLength(string hostRoot, string inputScheme, List<SmokeQuery> queries)
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Smoke_rlen_{Guid.NewGuid():N}");
        using var host = new ImeHostService(
            options,
            inputScheme: inputScheme,
            hostBaseDirectory: hostRoot,
            userDictionaryPath: Path.Combine(hostRoot, $"rlen-user-{inputScheme}.json"));
        ImeModuleRuntime.SetBridgeForTesting(new ImeHostBridge(options));
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());
        const string currentComposition = "n";
        const string source = "XiaoXiIme";
        const string expectedReading = "xiao xi ai mu yi";
        var expectedSize = ImeCompositionContextWriter.GetRequiredConversionListSize(
            [new ImeCandidate(source, expectedReading)],
            writeReading: true);
        var buffer = new byte[256];
        Array.Fill(buffer, (byte)0xCC);

        try
        {
            host.Start();
            fixed (char* compositionPointer = currentComposition)
            {
                var setResult = ImeExports.ImeSetCompositionStringManaged(
                    himc.Handle,
                    ImeConstants.ScsSetStr,
                    compositionPointer,
                    (uint)(currentComposition.Length * sizeof(char)),
                    null,
                    0);
                if (setResult != 1 || !string.Equals(himc.ReadCompositionText(), currentComposition, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Expected reverse-length setup composition '{currentComposition}'.");
                }
            }

            uint required;
            fixed (char* sourcePointer = source)
            fixed (byte* destination = buffer)
            {
                required = ImeExports.ImeConversionListManaged(
                    himc.Handle,
                    sourcePointer,
                    destination,
                    (uint)buffer.Length,
                    ImeConstants.GclReverseLength);
            }

            queries.Add(new SmokeQuery($"reverse-length:{inputScheme}:{source}", expectedSize.ToString(), required.ToString(), (int)required));
            if (required != expectedSize
                || buffer.Any(value => value != 0xCC)
                || !string.Equals(himc.ReadCompositionText(), currentComposition, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Expected reverse-length '{expectedSize}' for '{inputScheme}:{source}', got '{required}'.");
            }
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    private static unsafe void AssertImeEscape(string hostRoot, string inputScheme, string input, List<SmokeQuery> queries)
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Smoke_esc_{Guid.NewGuid():N}");
        using var host = new ImeHostService(
            options,
            inputScheme: inputScheme,
            hostBaseDirectory: hostRoot,
            userDictionaryPath: Path.Combine(hostRoot, $"esc-user-{inputScheme}.json"));
        ImeModuleRuntime.SetBridgeForTesting(new ImeHostBridge(options));
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());

        try
        {
            host.Start();
            fixed (char* compositionPointer = input)
            {
                var setResult = ImeExports.ImeSetCompositionStringManaged(
                    himc.Handle,
                    ImeConstants.ScsSetStr,
                    compositionPointer,
                    (uint)(input.Length * sizeof(char)),
                    null,
                    0);
                if (setResult != 1 || !string.Equals(himc.ReadCompositionText(), input, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Expected ime-escape setup composition '{input}'.");
                }
            }

            uint requested = ImeConstants.ImeEscImeName;
            var support = ImeExports.ImeEscapeManaged(himc.Handle, ImeConstants.ImeEscQuerySupport, &requested);
            var name = stackalloc char[ImeConstants.ImeNameBufferLength];
            var written = ImeExports.ImeEscapeManaged(himc.Handle, ImeConstants.ImeEscImeName, name);
            var actual = new string(name);
            queries.Add(new SmokeQuery($"ime-escape:{inputScheme}:{input}", ImeExportsContract.ImeDisplayName, actual, (int)support));
            if (support != 1
                || written != 1
                || !string.Equals(actual, ImeExportsContract.ImeDisplayName, StringComparison.Ordinal)
                || !string.Equals(himc.ReadCompositionText(), input, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Expected ime-escape '{ImeExportsContract.ImeDisplayName}' while composing '{input}', got '{actual}'.");
            }
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    private static unsafe void AssertRegisterWord(string hostRoot, string inputScheme, string input, List<SmokeQuery> queries)
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Smoke_reg_{Guid.NewGuid():N}");
        using var host = new ImeHostService(
            options,
            inputScheme: inputScheme,
            hostBaseDirectory: hostRoot,
            userDictionaryPath: Path.Combine(hostRoot, $"reg-user-{inputScheme}.json"));
        ImeModuleRuntime.SetBridgeForTesting(new ImeHostBridge(options));
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());

        try
        {
            host.Start();
            fixed (char* compositionPointer = input)
            {
                var setResult = ImeExports.ImeSetCompositionStringManaged(
                    himc.Handle,
                    ImeConstants.ScsSetStr,
                    compositionPointer,
                    (uint)(input.Length * sizeof(char)),
                    null,
                    0);
                if (setResult != 1 || !string.Equals(himc.ReadCompositionText(), input, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Expected register-word setup composition '{input}'.");
                }
            }

            var reading = "zidingyi";
            var text = "自定义";
            int registered;
            fixed (char* readingPointer = reading)
            fixed (char* textPointer = text)
            {
                registered = ImeExports.ImeRegisterWordManaged(readingPointer, ImeConstants.ImeRegWordStyleUserFirst, textPointer);
            }

            var style = stackalloc StyleBuf[1];
            var styleCount = ImeExports.ImeGetRegisterWordStyleManaged(1, style);
            string? enumeratedReading = null;
            string? enumeratedText = null;
            uint enumeratedStyle = 0;
            ImeExports.SetRegisterWordEnumProcForTesting((readingPointer, callbackStyle, textPointer, _) =>
            {
                enumeratedReading = new string(readingPointer);
                enumeratedText = new string(textPointer);
                enumeratedStyle = callbackStyle;
                return 1;
            });
            var enumerated = ImeExports.ImeEnumRegisterWordManaged(0, null, 0, null, null);
            queries.Add(new SmokeQuery($"register-word:{inputScheme}:{input}", text, enumeratedText ?? string.Empty, registered));
            queries.Add(new SmokeQuery($"register-word-enum:{inputScheme}:{input}", reading, enumeratedReading ?? string.Empty, (int)enumerated));
            if (registered != 1
                || styleCount != 1
                || style->Style != ImeConstants.ImeRegWordStyleUserFirst
                || !string.Equals(new string(style->Description), ImeExportsContract.UserWordStyleDescription, StringComparison.Ordinal)
                || enumerated != 1
                || !string.Equals(enumeratedReading, reading, StringComparison.Ordinal)
                || !string.Equals(enumeratedText, text, StringComparison.Ordinal)
                || enumeratedStyle != ImeConstants.ImeRegWordStyleUserFirst
                || !string.Equals(himc.ReadCompositionText(), input, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Expected register-word '{text}' while composing '{input}'.");
            }

            int unregistered;
            fixed (char* readingPointer = reading)
            fixed (char* textPointer = text)
            {
                unregistered = ImeExports.ImeUnregisterWordManaged(readingPointer, ImeConstants.ImeRegWordStyleUserFirst, textPointer);
            }

            enumeratedReading = null;
            enumeratedText = null;
            var remaining = ImeExports.ImeEnumRegisterWordManaged(0, null, 0, null, null);
            queries.Add(new SmokeQuery($"unregister-word:{inputScheme}:{input}", string.Empty, enumeratedText ?? string.Empty, unregistered));
            if (unregistered != 1
                || remaining != 0
                || enumeratedReading is not null
                || !string.Equals(himc.ReadCompositionText(), input, StringComparison.Ordinal)
                || himc.CompositionString->ResultStrLength != 0)
            {
                throw new InvalidOperationException($"Expected unregister-word to remove '{text}' while composing '{input}'.");
            }
        }
        finally
        {
            ImeExports.SetRegisterWordEnumProcForTesting(null);
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    private static unsafe void AssertImeConfigureRegisterWord(string hostRoot, string inputScheme, string input, List<SmokeQuery> queries)
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Smoke_cfg_{Guid.NewGuid():N}");
        using var host = new ImeHostService(
            options,
            inputScheme: inputScheme,
            hostBaseDirectory: hostRoot,
            userDictionaryPath: Path.Combine(hostRoot, $"cfg-user-{inputScheme}.json"));
        ImeModuleRuntime.SetBridgeForTesting(new ImeHostBridge(options));
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());

        try
        {
            host.Start();
            fixed (char* compositionPointer = input)
            {
                var setResult = ImeExports.ImeSetCompositionStringManaged(
                    himc.Handle,
                    ImeConstants.ScsSetStr,
                    compositionPointer,
                    (uint)(input.Length * sizeof(char)),
                    null,
                    0);
                if (setResult != 1 || !string.Equals(himc.ReadCompositionText(), input, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Expected ime-configure setup composition '{input}'.");
                }
            }

            var reading = "peizhi";
            var text = "配置词";
            int configured;
            fixed (char* readingPointer = reading)
            fixed (char* textPointer = text)
            {
                var registerWord = stackalloc RegisterWord[1];
                registerWord->Reading = readingPointer;
                registerWord->Word = textPointer;
                configured = ImeExports.ImeConfigureManaged(0, 0, ImeConstants.ImeConfigRegisterWord, registerWord);
            }

            string? enumeratedReading = null;
            string? enumeratedText = null;
            ImeExports.SetRegisterWordEnumProcForTesting((readingPointer, _, textPointer, _) =>
            {
                enumeratedReading = new string(readingPointer);
                enumeratedText = new string(textPointer);
                return 1;
            });
            var enumerated = ImeExports.ImeEnumRegisterWordManaged(0, null, 0, null, null);
            queries.Add(new SmokeQuery($"ime-configure:{inputScheme}:{input}", text, enumeratedText ?? string.Empty, configured));
            queries.Add(new SmokeQuery($"ime-configure-enum:{inputScheme}:{input}", reading, enumeratedReading ?? string.Empty, (int)enumerated));
            if (configured != 1
                || enumerated != 1
                || !string.Equals(enumeratedReading, reading, StringComparison.Ordinal)
                || !string.Equals(enumeratedText, text, StringComparison.Ordinal)
                || !string.Equals(himc.ReadCompositionText(), input, StringComparison.Ordinal)
                || himc.CompositionString->ResultStrLength != 0)
            {
                throw new InvalidOperationException($"Expected ime-configure '{text}' while composing '{input}'.");
            }
        }
        finally
        {
            ImeExports.SetRegisterWordEnumProcForTesting(null);
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    private static void RequireQueries(List<SmokeQuery> queries, params string[] inputs)
    {
        foreach (var input in inputs)
        {
            if (!queries.Exists(query => string.Equals(query.Input, input, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException($"Missing smoke query '{input}'.");
            }
        }
    }

    private static unsafe void AssertToAsciiEx(string hostRoot, string inputScheme, string input, string expectedText, List<SmokeQuery> queries)
    {
        AssertImmPath(hostRoot, inputScheme, input, expectedText, eatKeysFirst: false, queries);
    }

    private static unsafe void AssertProcessKey(string hostRoot, string inputScheme, string input, string expectedText, List<SmokeQuery> queries)
    {
        AssertImmPath(hostRoot, inputScheme, input, expectedText, eatKeysFirst: true, queries);
    }

    private static unsafe void AssertImmPath(
        string hostRoot,
        string inputScheme,
        string input,
        string expectedText,
        bool eatKeysFirst,
        List<SmokeQuery> queries)
    {
        var pathName = eatKeysFirst ? ProcessKeyPathName : ToAsciiPathName;
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Smoke_{pathName}_{Guid.NewGuid():N}");
        using var host = new ImeHostService(
            options,
            inputScheme: inputScheme,
            hostBaseDirectory: hostRoot,
            userDictionaryPath: Path.Combine(hostRoot, $"{pathName}-user-{inputScheme}.json"));
        ImeModuleRuntime.SetBridgeForTesting(new ImeHostBridge(options));
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());
        var buffer = stackalloc byte[sizeof(TransMsgList) + sizeof(TransMsg)];
        var list = (TransMsgList*)buffer;

        try
        {
            host.Start();
            uint result = 0;
            foreach (var character in input)
            {
                var virtualKey = (uint)('A' + (character - 'a'));
                if (eatKeysFirst && !ImeExports.ImeProcessKeyManaged(himc.Handle, virtualKey, 0, null))
                {
                    throw new InvalidOperationException($"Expected ImeProcessKey to eat '{character}' for '{inputScheme}:{input}'.");
                }

                result = ImeExports.ImeToAsciiExManaged(virtualKey, 0, null, (nint)list, 0, himc.Handle);
            }

            var actual = ImeModuleRuntime.GetLastProcessResultForTesting().Snapshot.Candidates.Count == 0
                ? string.Empty
                : ImeModuleRuntime.GetLastProcessResultForTesting().Snapshot.Candidates[0].Text;
            queries.Add(new SmokeQuery($"{pathName}:{inputScheme}:{input}", expectedText, actual, (int)result));
            if (!string.Equals(actual, expectedText, StringComparison.Ordinal)
                || himc.CompositionString->CompStrLength != (uint)(input.Length * sizeof(char))
                || !string.Equals(himc.ReadCompositionText(), input, StringComparison.Ordinal)
                || himc.CandidateInfo->Count != 1
                || !string.Equals(himc.ReadFirstCandidateText(), expectedText, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Expected {pathName} '{expectedText}' for '{inputScheme}:{input}', got '{actual}'.");
            }

            if (eatKeysFirst && !ImeExports.ImeProcessKeyManaged(himc.Handle, ImeConstants.VkSpace, 0, null))
            {
                throw new InvalidOperationException($"Expected ImeProcessKey to eat space for '{inputScheme}:{input}'.");
            }

            var commit = ImeExports.ImeToAsciiExManaged(ImeConstants.VkSpace, 0, null, (nint)list, 0, himc.Handle);
            var lastResult = ImeModuleRuntime.GetLastProcessResultForTesting();
            queries.Add(new SmokeQuery($"{pathName}-commit:{inputScheme}:{input}", expectedText, lastResult.CommitText ?? string.Empty, (int)commit));
            if (!string.Equals(lastResult.CommitText, expectedText, StringComparison.Ordinal)
                || lastResult.Snapshot.IsComposing
                || commit != 2
                || list->Message.Message != ImeConstants.WmImeComposition
                || list->Message.WParam != expectedText[0]
                || list->Message.LParam != (nint)ImeConstants.GcsResultStr
                || (&list->Message)[1].Message != ImeConstants.WmImeEndComposition
                || himc.CompositionString->ResultStrLength != (uint)(expectedText.Length * sizeof(char))
                || !string.Equals(himc.ReadResultText(), expectedText, StringComparison.Ordinal)
                || himc.CompositionString->CompStrLength != 0)
            {
                throw new InvalidOperationException($"Expected {pathName} commit '{expectedText}' for '{inputScheme}:{input}', got '{lastResult.CommitText}'.");
            }
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    private static unsafe void AssertIndependentHimcSessions(string hostRoot, List<SmokeQuery> queries)
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Smoke_independent_{Guid.NewGuid():N}");
        using var host = new ImeHostService(
            options,
            inputScheme: DictionaryPackageLocations.FullPinyinInputScheme,
            hostBaseDirectory: hostRoot,
            userDictionaryPath: Path.Combine(hostRoot, "independent-himc-user.json"));
        ImeModuleRuntime.SetBridgeForTesting(new ImeHostBridge(options));
        using var first = new InMemoryImmContext();
        using var second = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(new CompositeImmContextAccessor(first, second).CreateWriter());
        var buffer = stackalloc byte[sizeof(TransMsgList) + sizeof(TransMsg)];
        var list = (TransMsgList*)buffer;

        try
        {
            host.Start();
            foreach (var character in "xiaoxiaimuyi")
            {
                var virtualKey = (uint)('A' + (character - 'a'));
                if (!ImeExports.ImeProcessKeyManaged(first.Handle, virtualKey, 0, null))
                {
                    throw new InvalidOperationException("Expected ImeProcessKey to eat the first HIMC composition.");
                }

                ImeExports.ImeToAsciiExManaged(virtualKey, 0, null, (nint)list, 0, first.Handle);
            }

            foreach (var character in "aimuyi")
            {
                var virtualKey = (uint)('A' + (character - 'a'));
                if (!ImeExports.ImeProcessKeyManaged(second.Handle, virtualKey, 0, null))
                {
                    throw new InvalidOperationException("Expected ImeProcessKey to eat the second HIMC composition.");
                }

                ImeExports.ImeToAsciiExManaged(virtualKey, 0, null, (nint)list, 0, second.Handle);
            }

            queries.Add(new SmokeQuery("independent-himc:fullPinyin:xiaoxiaimuyi", "XiaoXiIme", first.ReadFirstCandidateText(), (int)first.CandidateInfo->Count));
            if (!string.Equals(first.ReadCompositionText(), "xiaoxiaimuyi", StringComparison.Ordinal)
                || !string.Equals(first.ReadFirstCandidateText(), "XiaoXiIme", StringComparison.Ordinal)
                || !string.Equals(second.ReadCompositionText(), "aimuyi", StringComparison.Ordinal)
                || !string.Equals(second.ReadFirstCandidateText(), "IME", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected independent HIMC compositions to stay isolated.");
            }

            if (!ImeExports.ImeProcessKeyManaged(second.Handle, ImeConstants.VkSpace, 0, null))
            {
                throw new InvalidOperationException("Expected ImeProcessKey to eat space for the second HIMC.");
            }

            var commit = ImeExports.ImeToAsciiExManaged(ImeConstants.VkSpace, 0, null, (nint)list, 0, second.Handle);
            var lastResult = ImeModuleRuntime.GetLastProcessResultForTesting();
            queries.Add(new SmokeQuery("independent-himc-commit:fullPinyin:aimuyi", "IME", lastResult.CommitText ?? string.Empty, (int)commit));
            if (!string.Equals(lastResult.CommitText, "IME", StringComparison.Ordinal)
                || !string.Equals(second.ReadResultText(), "IME", StringComparison.Ordinal)
                || second.CompositionString->CompStrLength != 0
                || !string.Equals(first.ReadCompositionText(), "xiaoxiaimuyi", StringComparison.Ordinal)
                || !string.Equals(first.ReadFirstCandidateText(), "XiaoXiIme", StringComparison.Ordinal)
                || !ImeModuleRuntime.GetSnapshotForTesting(ImeSessionId.FromHimc(first.Handle)).IsComposing
                || ImeModuleRuntime.GetSnapshotForTesting(ImeSessionId.FromHimc(second.Handle)).IsComposing)
            {
                throw new InvalidOperationException("Expected the second HIMC to commit without clearing the first HIMC.");
            }
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    private static void AssertIndependentIpcSessions(string hostRoot, List<SmokeQuery> queries)
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Smoke_ipc_{Guid.NewGuid():N}");
        using var host = new ImeHostService(
            options,
            hostBaseDirectory: hostRoot,
            userDictionaryPath: Path.Combine(hostRoot, "independent-ipc-user.json"));
        using var client = new XiaoXiImeIpcClient(options);
        var firstSession = ImeSessionId.FromHimc(0x11);
        var secondSession = ImeSessionId.FromHimc(0x22);

        host.Start();
        client.ConnectAsync().GetAwaiter().GetResult();

        var first = ProcessCharacters(client, "xiaoxiaimuyi", firstSession);
        var second = ProcessCharacters(client, "aimuyi", secondSession);
        queries.Add(new SmokeQuery("independent-ipc:fullPinyin:xiaoxiaimuyi", "XiaoXiIme", first.Snapshot.Candidates[0].Text, first.Snapshot.Candidates.Count));
        queries.Add(new SmokeQuery("independent-ipc:fullPinyin:aimuyi", "IME", second.Snapshot.Candidates[0].Text, second.Snapshot.Candidates.Count));

        var lastActiveSnapshot = client.GetSnapshotAsync().GetAwaiter().GetResult();
        var lastActiveUiState = client.GetUiStateAsync().GetAwaiter().GetResult();
        var firstSnapshot = client.GetSnapshotAsync(firstSession).GetAwaiter().GetResult();
        var secondSnapshot = client.GetSnapshotAsync(secondSession).GetAwaiter().GetResult();
        if (!string.Equals(first.Snapshot.Composition.Reading, "xiaoxiaimuyi", StringComparison.Ordinal)
            || !string.Equals(first.Snapshot.Candidates[0].Text, "XiaoXiIme", StringComparison.Ordinal)
            || !string.Equals(second.Snapshot.Composition.Reading, "aimuyi", StringComparison.Ordinal)
            || !string.Equals(second.Snapshot.Candidates[0].Text, "IME", StringComparison.Ordinal)
            || !string.Equals(lastActiveSnapshot.Composition.Reading, "aimuyi", StringComparison.Ordinal)
            || !string.Equals(lastActiveSnapshot.Candidates[0].Text, "IME", StringComparison.Ordinal)
            || !string.Equals(lastActiveUiState.Composition.Reading, "aimuyi", StringComparison.Ordinal)
            || !string.Equals(lastActiveUiState.Candidates[0].Text, "IME", StringComparison.Ordinal)
            || !string.Equals(firstSnapshot.Composition.Reading, "xiaoxiaimuyi", StringComparison.Ordinal)
            || !string.Equals(secondSnapshot.Composition.Reading, "aimuyi", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Expected IPC sessions to stay isolated and empty snapshot requests to use the last active session.");
        }

        var commit = client.ProcessKeyAsync(new ImeKey(ImeKeyKind.Space), secondSession).GetAwaiter().GetResult();
        var firstAfterCommit = client.GetSnapshotAsync(firstSession).GetAwaiter().GetResult();
        var lastActiveAfterCommit = client.GetSnapshotAsync().GetAwaiter().GetResult();
        queries.Add(new SmokeQuery("independent-ipc-commit:fullPinyin:aimuyi", "IME", commit.CommitText ?? string.Empty, commit.Snapshot.Candidates.Count));
        queries.Add(new SmokeQuery("independent-ipc-fallback:fullPinyin:aimuyi", string.Empty, lastActiveAfterCommit.Composition.Reading, lastActiveAfterCommit.Candidates.Count));
        if (!string.Equals(commit.CommitText, "IME", StringComparison.Ordinal)
            || commit.Snapshot.IsComposing
            || !firstAfterCommit.IsComposing
            || !string.Equals(firstAfterCommit.Candidates[0].Text, "XiaoXiIme", StringComparison.Ordinal)
            || lastActiveAfterCommit.IsComposing)
        {
            throw new InvalidOperationException("Expected IPC commit of the second session to leave the first session composing.");
        }

        ProcessCharacters(client, "aimuyi", secondSession);
        var reset = client.ResetSessionAsync(secondSession).GetAwaiter().GetResult();
        var firstAfterReset = client.GetSnapshotAsync(firstSession).GetAwaiter().GetResult();
        var secondAfterReset = client.GetSnapshotAsync(secondSession).GetAwaiter().GetResult();
        queries.Add(new SmokeQuery("independent-ipc-reset:fullPinyin:xiaoxiaimuyi", "XiaoXiIme", firstAfterReset.Candidates[0].Text, firstAfterReset.Candidates.Count));
        if (!reset.Reset
            || reset.SessionId != secondSession
            || !firstAfterReset.IsComposing
            || !string.Equals(firstAfterReset.Candidates[0].Text, "XiaoXiIme", StringComparison.Ordinal)
            || secondAfterReset.IsComposing)
        {
            throw new InvalidOperationException("Expected IPC reset of the second session to leave the first session composing.");
        }
    }

    private static ImeProcessResult ProcessCharacters(XiaoXiImeIpcClient client, string input, ImeSessionId sessionId)
    {
        ImeProcessResult result = default!;
        foreach (var character in input)
        {
            result = client.ProcessKeyAsync(ImeKey.FromCharacter(character), sessionId).GetAwaiter().GetResult();
        }

        return result;
    }

    private static unsafe void AssertImeSelectCancel(string hostRoot, string inputScheme, string input, List<SmokeQuery> queries)
    {
        AssertNotifyControl(
            hostRoot,
            inputScheme,
            input,
            expectedText: string.Empty,
            queries,
            "ime-select-cancel",
            himc => ImeExports.ImeSelectManaged(himc.Handle, 0) == 1,
            expectCommit: false,
            expectHostSessionReset: true);
    }

    private static unsafe void AssertImeSetActiveContextCancel(string hostRoot, string inputScheme, string input, List<SmokeQuery> queries)
    {
        AssertNotifyControl(
            hostRoot,
            inputScheme,
            input,
            expectedText: string.Empty,
            queries,
            "ime-set-active-cancel",
            himc => ImeExports.ImeSetActiveContextManaged(himc.Handle, 0) == 1,
            expectCommit: false,
            expectHostSessionReset: true);
    }

    private static unsafe void AssertNotifyImeCancel(string hostRoot, string inputScheme, string input, List<SmokeQuery> queries)
    {
        AssertNotifyControl(
            hostRoot,
            inputScheme,
            input,
            expectedText: string.Empty,
            queries,
            "notify-cancel",
            himc => ImeExports.NotifyImeManaged(himc.Handle, ImeConstants.NiCompositionStr, 0, ImeConstants.CpsCancel) == 1,
            expectCommit: false);
    }

    private static unsafe void AssertNotifyImeComplete(string hostRoot, string inputScheme, string input, string expectedText, List<SmokeQuery> queries)
    {
        AssertNotifyControl(
            hostRoot,
            inputScheme,
            input,
            expectedText,
            queries,
            "notify-complete",
            himc => ImeExports.NotifyImeManaged(himc.Handle, ImeConstants.NiCompositionStr, 0, ImeConstants.CpsComplete) == 1,
            expectCommit: true);
    }

    private static unsafe void AssertNotifyImeSelectCandidate(string hostRoot, string inputScheme, string input, string expectedText, List<SmokeQuery> queries)
    {
        AssertNotifyControl(
            hostRoot,
            inputScheme,
            input,
            expectedText,
            queries,
            "notify-select",
            himc => ImeExports.NotifyImeManaged(himc.Handle, ImeConstants.NiSelectCandidateStr, 0, 0) == 1,
            expectCommit: true);
    }

    private static unsafe void AssertNotifyImeRevert(string hostRoot, string inputScheme, string input, List<SmokeQuery> queries)
    {
        AssertNotifyControl(
            hostRoot,
            inputScheme,
            input,
            expectedText: input,
            queries,
            "notify-revert",
            himc => ImeExports.NotifyImeManaged(himc.Handle, ImeConstants.NiCompositionStr, 0, ImeConstants.CpsRevert) == 1,
            expectCommit: true);
    }

    private static unsafe void AssertNotifyControl(
        string hostRoot,
        string inputScheme,
        string input,
        string expectedText,
        List<SmokeQuery> queries,
        string pathName,
        Func<InMemoryImmContext, bool> control,
        bool expectCommit,
        bool expectHostSessionReset = false)
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Smoke_{pathName}_{Guid.NewGuid():N}");
        using var host = new ImeHostService(
            options,
            inputScheme: inputScheme,
            hostBaseDirectory: hostRoot,
            userDictionaryPath: Path.Combine(hostRoot, $"{pathName}-user-{inputScheme}.json"));
        ImeModuleRuntime.SetBridgeForTesting(new ImeHostBridge(options));
        using var himc = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(himc.CreateWriter());
        var buffer = stackalloc byte[sizeof(TransMsgList) + sizeof(TransMsg)];
        var list = (TransMsgList*)buffer;

        try
        {
            host.Start();
            foreach (var character in input)
            {
                var virtualKey = (uint)('A' + (character - 'a'));
                if (!ImeExports.ImeProcessKeyManaged(himc.Handle, virtualKey, 0, null))
                {
                    throw new InvalidOperationException($"Expected ImeProcessKey to eat '{character}' for '{inputScheme}:{input}'.");
                }

                ImeExports.ImeToAsciiExManaged(virtualKey, 0, null, (nint)list, 0, himc.Handle);
            }

            if (!string.Equals(himc.ReadCompositionText(), input, StringComparison.Ordinal)
                || !string.Equals(himc.ReadFirstCandidateText(), "XiaoXiIme", StringComparison.Ordinal)
                || !control(himc))
            {
                throw new InvalidOperationException($"Expected {pathName} to succeed for '{inputScheme}:{input}'.");
            }

            var lastResult = ImeModuleRuntime.GetLastProcessResultForTesting();
            var actual = lastResult.CommitText ?? string.Empty;
            queries.Add(new SmokeQuery($"{pathName}:{inputScheme}:{input}", expectedText, actual, (int)himc.CandidateInfo->Count));
            if (lastResult.Snapshot.IsComposing
                || himc.CompositionString->CompStrLength != 0
                || himc.CandidateInfo->Count != 0
                || (expectCommit
                    ? !string.Equals(actual, expectedText, StringComparison.Ordinal) || !string.Equals(himc.ReadResultText(), expectedText, StringComparison.Ordinal)
                    : actual.Length != 0 || himc.CompositionString->ResultStrLength != 0)
                || (expectHostSessionReset
                    && (ImeModuleRuntime.GetSnapshotForTesting(ImeSessionId.FromHimc(himc.Handle)).IsComposing
                        || host.GetSnapshotAsync(ImeSessionId.FromHimc(himc.Handle)).GetAwaiter().GetResult().IsComposing)))
            {
                throw new InvalidOperationException($"Expected {pathName} '{expectedText}' for '{inputScheme}:{input}', got '{actual}'.");
            }
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    private static unsafe void AssertImeDestroyResetsSessions(string hostRoot, List<SmokeQuery> queries)
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Smoke_destroy_{Guid.NewGuid():N}");
        using var host = new ImeHostService(
            options,
            hostBaseDirectory: hostRoot,
            userDictionaryPath: Path.Combine(hostRoot, "ime-destroy-user.json"));
        ImeModuleRuntime.SetBridgeForTesting(new ImeHostBridge(options));
        using var first = new InMemoryImmContext();
        using var second = new InMemoryImmContext();
        ImeExports.SetCompositionContextWriterForTesting(new CompositeImmContextAccessor(first, second).CreateWriter());
        var buffer = stackalloc byte[sizeof(TransMsgList) + sizeof(TransMsg)];
        var list = (TransMsgList*)buffer;

        try
        {
            host.Start();
            foreach (var character in "xiaoxiaimuyi")
            {
                var virtualKey = (uint)('A' + (character - 'a'));
                if (!ImeExports.ImeProcessKeyManaged(first.Handle, virtualKey, 0, null))
                {
                    throw new InvalidOperationException("Expected ImeProcessKey to eat the first HIMC composition before ImeDestroy.");
                }

                ImeExports.ImeToAsciiExManaged(virtualKey, 0, null, (nint)list, 0, first.Handle);
            }

            foreach (var character in "aimuyi")
            {
                var virtualKey = (uint)('A' + (character - 'a'));
                if (!ImeExports.ImeProcessKeyManaged(second.Handle, virtualKey, 0, null))
                {
                    throw new InvalidOperationException("Expected ImeProcessKey to eat the second HIMC composition before ImeDestroy.");
                }

                ImeExports.ImeToAsciiExManaged(virtualKey, 0, null, (nint)list, 0, second.Handle);
            }

            if (ImeExports.ImeDestroyManaged(0) != 1)
            {
                throw new InvalidOperationException("Expected ImeDestroy to succeed.");
            }

            var firstSnapshot = host.GetSnapshotAsync(ImeSessionId.FromHimc(first.Handle)).GetAwaiter().GetResult();
            var secondSnapshot = host.GetSnapshotAsync(ImeSessionId.FromHimc(second.Handle)).GetAwaiter().GetResult();
            queries.Add(new SmokeQuery("ime-destroy:fullPinyin:xiaoxiaimuyi", string.Empty, firstSnapshot.Composition.Reading, firstSnapshot.Candidates.Count));
            if (ImeModuleRuntime.GetSnapshotForTesting(ImeSessionId.FromHimc(first.Handle)).IsComposing
                || ImeModuleRuntime.GetSnapshotForTesting(ImeSessionId.FromHimc(second.Handle)).IsComposing
                || firstSnapshot.IsComposing
                || secondSnapshot.IsComposing)
            {
                throw new InvalidOperationException("Expected ImeDestroy to reset every HIMC session.");
            }
        }
        finally
        {
            ImeExports.SetCompositionContextWriterForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    private sealed record SmokeQuery(string Input, string ExpectedText, string Actual, int Count);
}
