using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using XiaoXiIme.Dictionary;

namespace XiaoXiIme.SandboxRunner;

internal static class DebugFuzzyValidation
{
    internal static string GetUiAssembly(string launcher) => Path.Combine
        (Path.GetDirectoryName(Path.GetFullPath(launcher))!, "Lib", "ImeUI", "XiaoXiIme.ImeUi.Avalonia.dll");

    internal static async Task<int> RunAsync
    (
        string uiPath, string outputDirectory, bool minimal = false, bool sixSchemes = false, bool experience = false,
        bool natural = false
    )
    {
        SetProcessDPIAware();
        uiPath = Path.GetFullPath(uiPath, AppContext.BaseDirectory);
        outputDirectory = Path.GetFullPath(outputDirectory, AppContext.BaseDirectory);
        Directory.CreateDirectory(outputDirectory);
        var bounds = Path.Combine(outputDirectory, "bounds.json");
        var settings = experience
            ? Path.Combine
            (
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "XiaoXiIme",
                "debug-product-settings.json"
            )
            : Path.Combine(outputDirectory, "settings.json");
        var package = experience
            ? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(uiPath))!, "Dictionaries")
            : Path.Combine(AppContext.BaseDirectory, "XiaoXiIme.DictionaryPackage");
        var results = new List<object>();
        var identities = new List<object>();
        Process? active = null;
        Task<string>? diagnosticStderr = null;
        var source = Path.Combine(AppContext.BaseDirectory, "data", "dictionaries");
        if (!experience && !Directory.Exists(package))
        {
            LocalDictionaryPackageManager.Update
            (
                new LocalDictionaryPackageUpdate
                {
                    PhoneticSourcePaths = Directory.GetFiles(source, "*.phonetic.tsv", SearchOption.AllDirectories),
                    ShapeSourcePaths = Directory.GetFiles(source, "*.shape.tsv", SearchOption.AllDirectories),
                    SymbolSourcePaths = Directory.GetFiles(source, "*.symbols.tsv", SearchOption.AllDirectories),
                    Parameters = new DictionaryPackageParameters { InputScheme = "fullPinyin" }
                }, package
            );
        }

        var schemes = new[]
        {
            "fullPinyin", "sogouDoublePinyin", "microsoftDoublePinyin", "abcDoublePinyin", "jiajiaDoublePinyin",
            "xiaoheDoublePinyin"
        };
        if (sixSchemes)
        {
            File.WriteAllText
            (
                Path.Combine(outputDirectory, "packages.json"), JsonSerializer.Serialize
                (
                    schemes.Select
                    (scheme =>
                        {
                            var directory = DictionaryPackageLocations.ResolvePackageDirectory(inputScheme: scheme);
                            var dictionary = DictionaryPackageLoader.Load(directory);
                            if (dictionary.InputScheme != scheme)
                            {
                                throw new InvalidOperationException("Package identity mismatch: " + scheme);
                            }

                            return new
                            {
                                scheme, directory, files = Directory.GetFiles
                                    (directory, "*", SearchOption.AllDirectories).Select
                                (file =>
                                    new { file, sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))) }
                                ).ToArray()
                            };
                        }
                    ), new JsonSerializerOptions { WriteIndented = true }
                )
            );
        }

        var payloadFiles = Directory.GetFiles(AppContext.BaseDirectory, "*", SearchOption.AllDirectories);
        File.WriteAllText
        (
            Path.Combine(outputDirectory, "payload-size.json"), JsonSerializer.Serialize
            (
                new
                {
                    fileCount = payloadFiles.Length,
                    bytes = payloadFiles.Sum(file => new FileInfo(file).Length),
                    files = payloadFiles.Select
                    (file => new
                        {
                            path = Path.GetRelativePath(AppContext.BaseDirectory, file),
                            bytes = new FileInfo(file).Length
                        }
                    ).ToArray()
                }
            )
        );
        foreach (var file in Directory.GetFiles(package, "*", SearchOption.AllDirectories)
                     .Append(uiPath).Append(GetUiAssembly(uiPath))
                     .Append(Environment.ProcessPath!).Append(typeof(DebugFuzzyValidation).Assembly.Location))
        {
            using var stream = File.OpenRead(file);
            identities.Add(new { file, sha256 = Convert.ToHexString(SHA256.HashData(stream)) });
        }

        File.WriteAllText(Path.Combine(outputDirectory, "identity.json"), JsonSerializer.Serialize(identities));
        var experienceStarts = 0;

        Process Start()
        {
            var info = new ProcessStartInfo(Path.GetFullPath(uiPath))
                { UseShellExecute = false, RedirectStandardError = true };
            if (experience)
            {
                info.WorkingDirectory = experienceStarts++ % 2 == 0
                    ? Path.GetTempPath()
                    : Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                if (experienceStarts % 2 == 0)
                {
                    info.ArgumentList.Add("--experience");
                }
            }
            else
            {
                info.ArgumentList.Add("--dictionary-debug");
                info.ArgumentList.Add("--debug-settings-path");
                info.ArgumentList.Add(settings);
            }

            if (!sixSchemes && !experience)
            {
                info.ArgumentList.Add("--debug-package-path");
                info.ArgumentList.Add(package);
            }

            if (!experience)
            {
                info.Environment["XIAOXIIME_DEBUG_PACKAGE"] = "obsolete-package-must-be-ignored";
                info.Environment["XIAOXIIME_INPUT_SCHEME"] = "obsolete-scheme-must-be-ignored";
            }

            info.Environment["XIAOXIIME_VALIDATION_BOUNDS"] = bounds;
            active = Process.Start(info) ?? throw new InvalidOperationException();
            diagnosticStderr = active.StandardError.ReadToEndAsync();
            var actualPath = active.MainModule?.FileName ?? throw new InvalidOperationException();
            var actualFiles = new[]
            {
                actualPath, GetUiAssembly(actualPath), Environment.ProcessPath!,
                typeof(DebugFuzzyValidation).Assembly.Location
            };
            File.WriteAllText
            (
                Path.Combine(outputDirectory, "actual-" + active.Id + ".json"), JsonSerializer.Serialize
                (
                    new
                    {
                        uiPid = active.Id, runnerPid = Environment.ProcessId,
                        cwd = info.WorkingDirectory, arguments = info.ArgumentList.ToArray(),
                        productEnvironmentOverrides = info.Environment.Keys.Where
                        (key => key.StartsWith
                                ("XIAOXIIME_", StringComparison.Ordinal) && key != "XIAOXIIME_VALIDATION_BOUNDS"
                        ).ToArray(),
                        files = actualFiles.Select
                        (file => new
                            { file, sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))) }
                        ).ToArray()
                    }
                )
            );
            return active;
        }

        var currentScheme = "fullPinyin";
        var schemeLabels = new[] { "全拼", "搜狗双拼", "微软双拼", "智能 ABC 双拼", "加加双拼", "小鹤双拼" };

        bool IsTarget(JsonElement control, string text, bool name) =>
            control.GetProperty(name ? "name" : "text").GetString() == text &&
            control.GetProperty("enabled").GetBoolean() &&
            (name || control.GetProperty("type").GetString() == "Button" ||
             (sixSchemes || natural) && (schemeLabels.Contains(text) || text == "自然码双拼"));

        async Task RawKey(byte key)
        {
            var inputs = new[]
            {
                new NativeInput { Type = 1, Keyboard = new KeyboardInput { VirtualKey = key } },
                new NativeInput { Type = 1, Keyboard = new KeyboardInput { VirtualKey = key, Flags = 2 } }
            };
            if (SendInput(2, inputs, Marshal.SizeOf<NativeInput>()) != 2)
            {
                throw new InvalidOperationException("Settings key delivery failed");
            }

            await Task.Delay(250);
        }

        async Task Click(string text, bool name = false)
        {
            for (var i = 0; i < 150; i++)
            {
                if (File.Exists(bounds))
                {
                    using var json = JsonDocument.Parse(ReadBounds(bounds));
                    if (json.RootElement.GetProperty("pid").GetInt32() != active?.Id)
                    {
                        throw new InvalidOperationException("Bounds PID differs from active UI PID");
                    }

                    var mainHandle = (nint)json.RootElement.GetProperty("controls").EnumerateArray()
                        .First(c => c.GetProperty("name").GetString() == "DebugOutput").GetProperty
                            ("handle").GetInt64();
                    var control = json.RootElement.GetProperty("controls").EnumerateArray().FirstOrDefault
                    (c =>
                        IsTarget(c, text, name) &&
                        (text is not ("DictionaryDebugSettings" or "DictionaryDebugPreviousPage"
                             or "DictionaryDebugNextPage" or "DebugOutput") ||
                         c.GetProperty("handle").GetInt64() == (long)mainHandle)
                    );
                    if (control.ValueKind != JsonValueKind.Undefined)
                    {
                        var handle = (nint)control.GetProperty("handle").GetInt64();
                        if (IsIconic(handle))
                        {
                            ShowWindow(handle, 9);
                        }

                        if (GetForegroundWindow() != handle)
                        {
                            SetForegroundWindow(handle);
                        }

                        if (text == "DebugOutput")
                        {
                            PostMessage(handle, 0x0050, 0, LoadKeyboardLayout("00000409", 0));
                        }

                        await Task.Delay(300);
                        using var currentBounds = JsonDocument.Parse(ReadBounds(bounds));
                        if (currentBounds.RootElement.GetProperty("pid").GetInt32() != active?.Id)
                        {
                            throw new InvalidOperationException("Current bounds PID differs from active UI PID");
                        }

                        control = currentBounds.RootElement.GetProperty("controls").EnumerateArray().FirstOrDefault
                        (c =>
                            IsTarget(c, text, name) && c.GetProperty("handle").GetInt64() == (long)handle
                        );
                        if (control.ValueKind == JsonValueKind.Undefined)
                        {
                            throw new InvalidOperationException
                                ("Target disappeared after activation: " + text + "; debug=" + Debug().GetRawText());
                        }

                        if (!control.GetProperty("hittable").GetBoolean())
                        {
                            if (!text.StartsWith("Fuzzy", StringComparison.Ordinal))
                            {
                                throw new InvalidOperationException("Avalonia hit-test does not reach target: " + text);
                            }

                            var window = currentBounds.RootElement.GetProperty("windows").EnumerateArray()
                                .First(w => w.GetProperty("handle").GetInt64() == (long)handle);
                            var position = window.GetProperty("Position");
                            var size = window.GetProperty("ClientSize");
                            SetCursorPos
                            (
                                position.GetProperty("X").GetInt32() + (int)(size.GetProperty("Width").GetDouble() / 2),
                                position.GetProperty("Y").GetInt32() + (int)(size.GetProperty("Height").GetDouble() / 2)
                            );
                            var wheel = new NativeInput
                                { Type = 0, Mouse = new MouseInput { Flags = 0x0800, Data = unchecked((uint)-120) } };
                            if (SendInput(1, new[] { wheel }, Marshal.SizeOf<NativeInput>()) != 1)
                            {
                                throw new InvalidOperationException("Scroll SendInput failed");
                            }

                            File.AppendAllText
                            (
                                Path.Combine(outputDirectory, "input.jsonl"),
                                JsonSerializer.Serialize(new { operation = "scroll", text }) + Environment.NewLine
                            );
                            await Task.Delay(300);
                            continue;
                        }

                        var beforePage = currentBounds.RootElement.GetProperty("debug")[0].GetProperty
                            ("page").GetInt32();
                        var beforeMask = text.StartsWith("Fuzzy", StringComparison.Ordinal)
                            ? currentBounds.RootElement.GetProperty("settings")[0].GetProperty("draft").GetProperty
                                ("FuzzyRuleMask").GetInt32()
                            : (int?)null;
                        GetWindowThreadProcessId(handle, out var targetPid);
                        if (targetPid != active?.Id || GetForegroundWindow() != handle)
                        {
                            throw new InvalidOperationException("Click target is not the active foreground UI window");
                        }

                        File.AppendAllText
                        (
                            Path.Combine(outputDirectory, "input.jsonl"), JsonSerializer.Serialize
                            (
                                new
                                {
                                    operation = "click", text, hwnd = (long)handle, pid = targetPid,
                                    generation = currentBounds.RootElement.GetProperty("generation").GetInt64(),
                                    snapshotTime = currentBounds.RootElement.GetProperty("time").GetDateTimeOffset(),
                                    foreground = (long)GetForegroundWindow(), x = control.GetProperty("x").GetInt32(),
                                    y = control.GetProperty("y").GetInt32()
                                }
                            ) + Environment.NewLine
                        );
                        var point = new NativePoint
                            { X = control.GetProperty("x").GetInt32(), Y = control.GetProperty("y").GetInt32() };
                        if (!SetCursorPos(point.X, point.Y))
                        {
                            throw new InvalidOperationException("Cannot move cursor to current control bounds");
                        }

                        await Task.Delay(100);
                        var hit = GetAncestor(WindowFromPoint(point), 2);
                        File.AppendAllText
                        (
                            Path.Combine(outputDirectory, "input.jsonl"), JsonSerializer.Serialize
                            (
                                new
                                {
                                    operation = "mouse-hit", text, target = (long)handle, hit = (long)hit, point.X,
                                    point.Y
                                }
                            ) + Environment.NewLine
                        );
                        if (hit != handle)
                        {
                            throw new InvalidOperationException
                                ("Actual control click is occluded by another window: " + text);
                        }

                        var mouseInputs = new[]
                        {
                            new NativeInput { Type = 0, Mouse = new MouseInput { Flags = 2 } },
                            new NativeInput { Type = 0, Mouse = new MouseInput { Flags = 4 } }
                        };
                        var mouseSent = SendInput(1, new[] { mouseInputs[0] }, Marshal.SizeOf<NativeInput>());
                        await Task.Delay(100);
                        mouseSent += SendInput(1, new[] { mouseInputs[1] }, Marshal.SizeOf<NativeInput>());
                        var mouseError = Marshal.GetLastWin32Error();
                        File.AppendAllText
                        (
                            Path.Combine(outputDirectory, "input.jsonl"), JsonSerializer.Serialize
                            (
                                new
                                {
                                    operation = "mouse-send", text, requested = 2, sent = mouseSent, error = mouseError
                                }
                            ) + Environment.NewLine
                        );
                        if (mouseSent != 2)
                        {
                            throw new InvalidOperationException("Mouse SendInput did not deliver both events: " + text);
                        }

                        await Task.Delay(700);
                        File.AppendAllText
                        (
                            Path.Combine(outputDirectory, "input.jsonl"), JsonSerializer.Serialize
                            (
                                new
                                {
                                    operation = "click-ack", text, beforePage,
                                    afterPage = Debug().GetProperty("page").GetInt32(),
                                    beforeGeneration = currentBounds.RootElement.GetProperty("generation").GetInt64(),
                                    afterSnapshot = JsonSerializer.Deserialize<JsonElement>(ReadBounds(bounds))
                                }
                            ) + Environment.NewLine
                        );
                        File.WriteAllText(Path.Combine(outputDirectory, "last-click-bounds.json"), ReadBounds(bounds));
                        if (beforeMask is not null)
                        {
                            using var afterClick = JsonDocument.Parse(ReadBounds(bounds));
                            var afterMask = afterClick.RootElement.GetProperty("settings")[0].GetProperty
                                ("draft").GetProperty("FuzzyRuleMask").GetInt32();
                            File.AppendAllText
                            (
                                Path.Combine(outputDirectory, "input.jsonl"), JsonSerializer.Serialize
                                (
                                    new
                                    {
                                        operation = "checkbox-ack", text, beforeMask, afterMask
                                    }
                                ) + Environment.NewLine
                            );
                            if (afterMask == beforeMask)
                            {
                                throw new InvalidOperationException
                                    ("Actual checkbox click did not change draft: " + text);
                            }
                        }

                        return;
                    }
                }

                await Task.Delay(200);
            }

            throw new TimeoutException(text);
        }

        async Task Key(byte key)
        {
            using var snapshot = JsonDocument.Parse(ReadBounds(bounds));
            var before = snapshot.RootElement.GetProperty("debug")[0].Clone();
            var handle = (nint)snapshot.RootElement.GetProperty("controls").EnumerateArray()
                .First(c => c.GetProperty("name").GetString() == "DebugOutput").GetProperty("handle").GetInt64();
            GetWindowThreadProcessId(handle, out var pid);
            if (pid != active?.Id || snapshot.RootElement.GetProperty("pid").GetInt32() != active?.Id ||
                GetForegroundWindow() != handle || before.GetProperty("settingsActive").GetBoolean() ||
                !before.GetProperty("focused").GetBoolean())
            {
                throw new InvalidOperationException("Keyboard target identity, foreground or focus mismatch");
            }

            var inputs = new[]
            {
                new NativeInput { Type = 1, Keyboard = new KeyboardInput { VirtualKey = key } },
                new NativeInput { Type = 1, Keyboard = new KeyboardInput { VirtualKey = key, Flags = 2 } }
            };
            var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeInput>());
            var error = Marshal.GetLastPInvokeError();
            File.AppendAllText
            (
                Path.Combine(outputDirectory, "input.jsonl"), JsonSerializer.Serialize
                (
                    new
                    {
                        operation = "key", key, hwnd = (long)handle, pid, foreground = (long)GetForegroundWindow(),
                        requested = inputs.Length, sent, error, before
                    }
                ) + Environment.NewLine
            );
            if (sent != inputs.Length)
            {
                throw new InvalidOperationException($"SendInput returned {sent}/2; Win32 error {error}");
            }

            var reading = before.GetProperty("reading").GetString() ?? "";
            var caret = before.GetProperty("caret").GetInt32();
            await WaitDebug
            (
                state => key switch
                {
                    >= 0x41 and <= 0x5A => state.GetProperty("reading").GetString() == reading.Insert
                                               (caret, ((char)(key + 32)).ToString()) &&
                                           state.GetProperty("caret").GetInt32() == caret + 1,
                    >= 0x31 and <= 0x39 => state.GetProperty("reading").GetString() != reading &&
                                           state.GetProperty("output").GetString() != before.GetProperty
                                               ("output").GetString(),
                    0xBA => state.GetProperty("reading").GetString() ==
                            (currentScheme is "sogouDoublePinyin" or "microsoftDoublePinyin"
                                ? reading.Insert(caret, ";")
                                : reading),
                    0x25 => state.GetProperty("caret").GetInt32() == Math.Max(0, caret - 1),
                    0x1B => state.GetProperty("reading").GetString() == "",
                    0x0D or 0x20 => state.GetProperty("reading").GetString() == "" &&
                                    state.GetProperty("output").GetString() != before.GetProperty("output").GetString(),
                    0x24 => state.GetProperty("page").GetInt32() == 1,
                    0x22 => state.GetProperty("page").GetInt32() == before.GetProperty("page").GetInt32() + 1,
                    _ => throw new InvalidOperationException($"No state acknowledgement defined for key {key}")
                }, $"Key {key}: expected state acknowledgement; SendInput={sent}, Win32={error}"
            );
            File.AppendAllText
            (
                Path.Combine(outputDirectory, "input.jsonl"), JsonSerializer.Serialize
                (
                    new
                    {
                        operation = "key-ack", key, after = Debug()
                    }
                ) + Environment.NewLine
            );
        }

        async Task WaitDebug(Func<JsonElement, bool> condition, string operation)
        {
            for (var attempt = 0; attempt < 150; attempt++)
            {
                if (condition(Debug()))
                {
                    return;
                }

                await Task.Delay(200);
            }

            throw new TimeoutException(operation);
        }

        async Task OpenSettings()
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                await Click("DictionaryDebugSettings", true);
                for (var poll = 0; poll < 15; poll++)
                {
                    if (Debug().GetProperty("settingsActive").GetBoolean())
                    {
                        return;
                    }

                    await Task.Delay(200);
                }
            }

            throw new TimeoutException("Open settings after three actual clicks");
        }

        async Task Type(string text)
        {
            foreach (var ch in text)
            {
                await Key((byte)char.ToUpperInvariant(ch));
            }

            await Task.Delay(1500);
        }

        JsonElement Debug() => JsonSerializer.Deserialize<JsonElement>(ReadBounds(bounds)).GetProperty("debug")[0];

        async Task WaitApplied(int mask)
        {
            for (var i = 0; i < 300; i++)
            {
                var debug = Debug();
                if (File.Exists(settings) && debug.GetProperty("rulemask").GetInt32() == mask &&
                    debug.GetProperty("engine").GetProperty("contextmask").GetInt32() == mask &&
                    JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(settings)).GetProperty
                        ("fuzzyRuleMask").GetInt32() == mask)
                {
                    return;
                }

                await Task.Delay(200);
            }

            throw new TimeoutException("Apply masks");
        }

        async Task FirstPage()
        {
            while (Debug().GetProperty("page").GetInt32() > 1)
            {
                var expectedPage = Debug().GetProperty("page").GetInt32() - 1;
                await Click("DictionaryDebugPreviousPage", true);
                await WaitDebug
                    (state => state.GetProperty("page").GetInt32() == expectedPage, "Previous candidate page");
            }
        }

        async Task Capture
            (string check, string reading, string? contains = null, int? caret = null, string? output = null)
        {
            File.AppendAllText
            (
                Path.Combine(outputDirectory, "phases.jsonl"),
                JsonSerializer.Serialize
                    (new { phase = check, stage = "start", utc = DateTimeOffset.UtcNow }) + Environment.NewLine
            );
            await Task.Delay(500);
            File.WriteAllText(Path.Combine(outputDirectory, check + ".json"), ReadBounds(bounds));
            if (File.Exists(settings))
            {
                File.Copy(settings, Path.Combine(outputDirectory, check + "-persist.json"), true);
            }

            var debug = Debug();
            var engine = debug.GetProperty("engine").GetProperty("engineCandidates").EnumerateArray().ToArray();
            var found = contains is null || engine.Any
            (c => c.GetProperty("Text").GetString() == contains &&
                  c.GetProperty("Reading").GetString() ==
                  (contains == "张" ? "zhang" : contains == "转身" ? "zhuan shen" : reading)
            );
            var pagesMatch = true;
            var seen = new List<(string? Text, string? Reading)>();
            if (contains is not null)
            {
                await FirstPage();
                var pages = Debug().GetProperty("totalPages").GetInt32();
                for (var page = 1; page <= pages; page++)
                {
                    debug = Debug();
                    pagesMatch &= debug.GetProperty("page").GetInt32() == page;
                    File.WriteAllText
                        (Path.Combine(outputDirectory, check + "-page-" + page + ".json"), ReadBounds(bounds));
                    foreach (var candidate in debug.GetProperty("candidates").EnumerateArray())
                    {
                        seen.Add
                            ((candidate.GetProperty("Text").GetString(), candidate.GetProperty("Reading").GetString()));
                    }

                    if (page < pages)
                    {
                        await Click("DictionaryDebugNextPage", true);
                        var expectedPage = page + 1;
                        await WaitDebug
                            (state => state.GetProperty("page").GetInt32() == expectedPage, "Next candidate page");
                    }
                }

                pagesMatch &= seen.SequenceEqual
                    (engine.Select(c => (c.GetProperty("Text").GetString(), c.GetProperty("Reading").GetString())));
                await FirstPage();
                debug = Debug();
            }

            var expectedMask = check is "applied" or "zang" or "multi-syllable" or "cancelled" or "reopened" ? 5 : 0;
            var masksMatch = debug.GetProperty("rulemask").GetInt32() == expectedMask &&
                             debug.GetProperty("engine").GetProperty("contextmask").GetInt32() == expectedMask;
            var persistedMask = File.Exists
                (settings)
                ? JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(settings)).GetProperty
                    ("fuzzyRuleMask").GetInt32()
                : 0;
            masksMatch &= persistedMask == expectedMask;
            results.Add
            (
                new
                {
                    check, found, pagesMatch, masksMatch, persistedMask,
                    actualReading = debug.GetProperty("reading").GetString(),
                    actualCaret = debug.GetProperty("caret").GetInt32(),
                    actualOutput = debug.GetProperty("output").GetString(), passed =
                        debug.GetProperty("reading").GetString() == reading && found && pagesMatch && masksMatch &&
                        (caret is null || debug.GetProperty("caret").GetInt32() == caret) &&
                        (output is null || debug.GetProperty("output").GetString() == output)
                }
            );
            CaptureDesktop(Path.Combine(outputDirectory, check + ".bmp"));
            File.WriteAllText(Path.Combine(outputDirectory, "report.json"), JsonSerializer.Serialize(results));
            File.AppendAllText
            (
                Path.Combine(outputDirectory, "phases.jsonl"),
                JsonSerializer.Serialize
                    (new { phase = check, stage = "end", utc = DateTimeOffset.UtcNow }) + Environment.NewLine
            );
        }

        async Task Close(Process process)
        {
            if (File.Exists(bounds))
            {
                using var snapshot = JsonDocument.Parse(File.ReadAllText(bounds));
                foreach (var window in snapshot.RootElement.GetProperty("windows").EnumerateArray()
                             .Where(w => w.GetProperty("type").GetString() == "DictionaryDebugWindow"))
                {
                    PostMessage((nint)window.GetProperty("handle").GetInt64(), 0x0010, 0, 0);
                }
            }
            else
            {
                process.CloseMainWindow();
            }

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                results.Add(new { check = "normal-exit-" + process.Id, passed = false });
                File.AppendAllText
                    (Path.Combine(outputDirectory, "normal-close-timeout.txt"), process.Id + Environment.NewLine);
                active = null;
                process.Dispose();
                return;
            }

            results.Add(new { check = "normal-exit-" + process.Id, passed = process.ExitCode == 0 });
            active = null;
            process.Dispose();
        }

        var rules = new[] { "ChC", "ShS", "ZhZ", "AngAn", "EngEn", "InIng", "IanIang", "UanUang", "AiAn", "UnOng" };
        try
        {
            {
                var ui = Start();
                if (experience)
                {
                    var startup = Stopwatch.StartNew();
                    while (true)
                    {
                        if (ui.HasExited)
                        {
                            throw new InvalidOperationException("UI exited during startup: " + ui.ExitCode);
                        }

                        if (File.Exists(bounds))
                        {
                            using var snapshot = JsonDocument.Parse(ReadBounds(bounds, requireFresh: false));
                            var root = snapshot.RootElement;
                            if (root.GetProperty("pid").GetInt32() == ui.Id &&
                                root.GetProperty("debug").GetArrayLength() > 0)
                            {
                                var state = root.GetProperty("debug")[0];
                                if (state.GetProperty("loaded").GetBoolean() &&
                                    state.GetProperty("engine").ValueKind == JsonValueKind.Object)
                                {
                                    File.WriteAllText
                                    (
                                        Path.Combine(outputDirectory, "startup-ready.json"),
                                        snapshot.RootElement.GetRawText()
                                    );
                                    _ = ReadBounds(bounds);
                                    results.Add(new { check = "experience-loaded-engine-available", passed = true });
                                    break;
                                }
                            }
                        }

                        if (startup.Elapsed > TimeSpan.FromSeconds(60))
                        {
                            throw new TimeoutException
                                ("Startup did not reach loaded and engine available within 60 seconds");
                        }

                        await Task.Delay(200);
                    }
                }
                else
                {
                    await Task.Delay(5000);
                }

                await Click("DebugOutput", true);
                if (experience)
                {
                    void SentenceCheck(string check, bool passed)
                    {
                        results.Add(new { check, passed });
                        File.WriteAllText
                            (Path.Combine(outputDirectory, "report.json"), JsonSerializer.Serialize(results));
                        File.WriteAllText(Path.Combine(outputDirectory, check + ".json"), ReadBounds(bounds));
                        CaptureDesktop(Path.Combine(outputDirectory, check + ".bmp"));
                        if (!passed)
                        {
                            throw new InvalidOperationException("Acceptance assertion failed: " + check);
                        }
                    }

                    if (natural)
                    {
                        await OpenSettings();
                        using (var selection = JsonDocument.Parse(ReadBounds(bounds)))
                        {
                            var selected = selection.RootElement.GetProperty("settings")[0].GetProperty
                                ("draft").GetProperty("InputScheme").GetInt32();
                            if (selected != 6)
                            {
                                await Click(schemeLabels[Array.IndexOf(new[] { 0, 2, 3, 4, 5, 1 }, selected)]);
                                await RawKey(0x24);
                                var selectable = PhoneticSchemeCatalog.All.Where
                                    (scheme => scheme.ProductScheme.HasValue).ToArray();
                                var index = Array.FindIndex
                                    (selectable, scheme => scheme.SchemeId == "naturalCodeDoublePinyin");
                                for (var step = 0; step < index; step++)
                                {
                                    await RawKey(0x28);
                                }

                                await RawKey(0x0D);
                            }
                        }

                        await WaitDebug
                        (
                            _ =>
                            {
                                using var draft = JsonDocument.Parse(ReadBounds(bounds));
                                return draft.RootElement.GetProperty("settings")[0].GetProperty("draft").GetProperty
                                    ("InputScheme").GetInt32() == 6;
                            }, "Natural code actual draft selection"
                        );
                        SentenceCheck("natural-settings-draft", true);
                        int naturalMask;
                        using (var fuzzyDraft = JsonDocument.Parse(ReadBounds(bounds)))
                        {
                            var mask = fuzzyDraft.RootElement.GetProperty("settings")[0].GetProperty
                                ("draft").GetProperty("FuzzyRuleMask").GetInt32();
                            naturalMask = mask | 2;
                            if ((mask & 2) == 0)
                            {
                                await Click("FuzzyShS", true);
                            }
                        }

                        await WaitDebug
                        (
                            _ =>
                            {
                                using var draft = JsonDocument.Parse(ReadBounds(bounds));
                                return (draft.RootElement.GetProperty("settings")[0].GetProperty("draft").GetProperty
                                    ("FuzzyRuleMask").GetInt32() & 2) != 0;
                            }, "Natural code ShS actual draft enabled"
                        );
                        SentenceCheck("natural-shs-draft", true);
                        using (var draft = JsonDocument.Parse(ReadBounds(bounds)))
                        {
                            if (draft.RootElement.GetProperty("settings")[0].GetProperty("dirty").GetBoolean())
                            {
                                await Click("应用");
                            }
                        }

                        await WaitDebug
                        (
                            state =>
                            {
                                using var applied = JsonDocument.Parse(ReadBounds(bounds));
                                var actual = applied.RootElement.GetProperty("settings")[0];
                                return state.GetProperty("loaded").GetBoolean() &&
                                       !actual.GetProperty("busy").GetBoolean() &&
                                       actual.GetProperty("baseline").GetProperty("InputScheme").GetInt32() == 6 &&
                                       (actual.GetProperty("baseline").GetProperty
                                           ("FuzzyRuleMask").GetInt32() & 2) != 0 &&
                                       !actual.GetProperty("dirty").GetBoolean();
                            }, "Natural code applied state"
                        );
                        SentenceCheck("natural-settings-applied", true);
                        await Click("取消");
                        currentScheme = "naturalCodeDoublePinyin";
                        await Click("DebugOutput", true);
                        await WaitApplied(naturalMask);
                        await Type("uohwrfuo");
                        var all = Debug().GetProperty("engine").GetProperty
                            ("engineCandidates").EnumerateArray().ToArray();

                        int Span(JsonElement candidate) => (candidate.GetProperty("Reading").GetString() ?? "").Split
                            (' ', StringSplitOptions.RemoveEmptyEntries).Length * 2;

                        SentenceCheck
                        (
                            "natural-eight-six-four-two", new[] { 8, 6, 4, 2 }.All
                                                              (length => all.Any(c => Span(c) == length)) &&
                                                          all.Any
                                                          (c => c.GetProperty("Text").GetString() == "说话人" &&
                                                                Span(c) == 6
                                                          ) &&
                                                          all.Any
                                                          (c => c.GetProperty("Text").GetString() == "说话" &&
                                                                Span(c) == 4
                                                          )
                        );
                        foreach (var length in new[] { 6, 4, 2, 8 })
                        {
                            if (Debug().GetProperty("reading").GetString() != "uohwrfuo")
                            {
                                await Key(0x1B);
                                await Type("uohwrfuo");
                            }

                            var local = -1;
                            while (local < 0)
                            {
                                local = Array.FindIndex
                                (
                                    Debug().GetProperty("candidates").EnumerateArray().ToArray(), c =>
                                        Span(c) == length &&
                                        (length == 6
                                            ? c.GetProperty("Text").GetString() == "说话人"
                                            : length != 4 || c.GetProperty("Text").GetString() == "说话")
                                );
                                if (local >= 0)
                                {
                                    break;
                                }

                                if (Debug().GetProperty("page").GetInt32() >=
                                    Debug().GetProperty("totalPages").GetInt32())
                                {
                                    throw new InvalidOperationException
                                        ("Required candidate missing from actual pages: " + length);
                                }

                                await Key(0x22);
                            }

                            SentenceCheck("natural-page-selection-" + length, true);
                            await Key((byte)(0x31 + local));
                            SentenceCheck
                            (
                                "natural-remaining-" + length,
                                Debug().GetProperty("reading").GetString() == "uohwrfuo"[length..]
                            );
                            if (length < 8)
                            {
                                await Type("uo");
                                SentenceCheck
                                (
                                    "natural-continued-" + length,
                                    Debug().GetProperty("reading").GetString() == "uohwrfuo"[length..] + "uo"
                                );
                            }
                        }

                        foreach (var input in new[] { "mz", "uohw" })
                        {
                            await Type(input);
                            var candidates = Debug().GetProperty("engine").GetProperty
                                ("engineCandidates").EnumerateArray().ToArray();
                            SentenceCheck
                            (
                                "natural-no-prediction-" + input, candidates.Length > 0 && candidates.All
                                (c => input == "mz"
                                    ? c.GetProperty("Reading").GetString() == "mei"
                                    : c.GetProperty("Reading").GetString()?.Split(' ').Length <= 2 &&
                                      c.GetProperty("Text").GetString() is not ("说话了" or "说话的人" or "方式")
                                )
                            );
                            await Key(0x1B);
                        }

                        await Close(ui);
                        if (diagnosticStderr is not null)
                        {
                            await File.WriteAllTextAsync
                                (Path.Combine(outputDirectory, "product-stderr.log"), await diagnosticStderr);
                        }

                        File.WriteAllText
                            (Path.Combine(outputDirectory, "report.json"), JsonSerializer.Serialize(results));
                        return results.All
                            (result => JsonSerializer.SerializeToElement(result).GetProperty("passed").GetBoolean())
                            ? 0
                            : 1;
                    }

                    await Type("bicu");
                    var sentenceCandidates = Debug().GetProperty("engine").GetProperty
                        ("engineCandidates").EnumerateArray().ToArray();
                    SentenceCheck
                    (
                        "experience-bicu-combination",
                        sentenceCandidates.Any(c => c.GetProperty("Reading").GetString() == "bi cu")
                    );
                    var localIndex = -1;
                    while (localIndex < 0)
                    {
                        var pageCandidates = Debug().GetProperty("candidates").EnumerateArray().ToArray();
                        localIndex = Array.FindIndex
                        (
                            pageCandidates,
                            c => c.GetProperty("Text").GetString() == "笔" &&
                                 c.GetProperty("Reading").GetString() == "bi"
                        );
                        if (localIndex >= 0)
                        {
                            break;
                        }

                        if (Debug().GetProperty("page").GetInt32() >= Debug().GetProperty("totalPages").GetInt32())
                        {
                            throw new InvalidOperationException("Local 笔 candidate not found in actual pages");
                        }

                        await Key(0x22);
                    }

                    await Key((byte)(0x31 + localIndex));
                    SentenceCheck
                    (
                        "experience-local-bi-remaining-cu", Debug().GetProperty("reading").GetString() == "cu" &&
                                                            Debug().GetProperty("output").GetString()?.EndsWith
                                                                ("笔", StringComparison.Ordinal) == true &&
                                                            Debug().GetProperty("candidates").GetArrayLength() > 0
                    );
                    await Key(0x31);
                    SentenceCheck
                        ("experience-cu-continued-selection", Debug().GetProperty("reading").GetString() == "");
                    await Type("be");
                    SentenceCheck
                    (
                        "experience-be-prefix-combination", Debug().GetProperty("engine").GetProperty
                            ("engineCandidates").EnumerateArray().Any
                        (c =>
                            c.GetProperty("Reading").GetString() is { } canonical &&
                            canonical.Split(' ') is [var head, var tail] &&
                            head.StartsWith("b", StringComparison.Ordinal) && tail.StartsWith
                                ("e", StringComparison.Ordinal)
                        )
                    );
                    await Key(0x1B);
                    await OpenSettings();
                    results.Add(new { check = "experience-settings-entry", passed = true });
                    await Click("取消");
                    await Close(ui);
                    File.WriteAllText
                        (Path.Combine(outputDirectory, "core-report.json"), JsonSerializer.Serialize(results));
                    File.WriteAllText(Path.Combine(outputDirectory, "report.json"), JsonSerializer.Serialize(results));
                    return results.All
                        (result => JsonSerializer.SerializeToElement(result).GetProperty("passed").GetBoolean())
                        ? 0
                        : 1;
                }

                if (sixSchemes)
                {
                    var enumValues = new[] { 0, 2, 3, 4, 5, 1 };
                    var codes = new[] { "nihao", "nihk", "nihk", "nihk", "nihd", "nihc" };

                    void Check(string check, bool passed)
                    {
                        results.Add(new { check, passed });
                        File.WriteAllText
                            (Path.Combine(outputDirectory, "report.json"), JsonSerializer.Serialize(results));
                        if (!passed)
                        {
                            throw new InvalidOperationException("Acceptance assertion failed: " + check);
                        }
                    }

                    for (var index = 0; index < schemes.Length; index++)
                    {
                        var scheme = schemes[index];
                        File.AppendAllText
                        (
                            Path.Combine(outputDirectory, "phases.jsonl"),
                            JsonSerializer.Serialize(new { scheme, stage = "start" }) + Environment.NewLine
                        );
                        await Type("ni");
                        await Key(0x0D);
                        await Type("ni");
                        await Key(0x25);
                        var before = Debug().Clone();
                        await OpenSettings();
                        if (index > 0)
                        {
                            await Click(schemeLabels[index - 1]);
                            await RawKey(0x24);
                            for (var step = 0; step < index; step++)
                            {
                                await RawKey(0x28);
                            }

                            await RawKey(0x0D);
                            using var draft = JsonDocument.Parse(ReadBounds(bounds));
                            Check
                            (
                                scheme + "-selected-draft",
                                draft.RootElement.GetProperty("settings")[0].GetProperty("draft").GetProperty
                                    ("InputScheme").GetInt32() == enumValues[index]
                            );
                        }

                        await Click("FuzzyZhZ", true);
                        await Click("应用");
                        await WaitApplied(4);
                        currentScheme = scheme;
                        using (var saved = JsonDocument.Parse(File.ReadAllText(settings)))
                        {
                            Check
                            (
                                scheme + "-persisted-scheme",
                                saved.RootElement.GetProperty("inputScheme").GetInt32() == enumValues[index]
                            );
                        }

                        await Click("取消");
                        var after = Debug();
                        File.WriteAllText(Path.Combine(outputDirectory, scheme + "-switched.json"), ReadBounds(bounds));
                        Check
                        (
                            scheme + "-active-state-preserved",
                            new[] { "reading", "caret", "output" }.All
                            (property =>
                                before.GetProperty(property).GetRawText() == after.GetProperty(property).GetRawText()
                            )
                        );
                        await OpenSettings();
                        await Click("FuzzyZhZ", true);
                        await Click("应用");
                        await WaitApplied(0);
                        await Click("取消");
                        await Key(0x1B);
                        await Type(codes[index]);
                        Check
                        (
                            scheme + "-real-candidate",
                            Debug().GetProperty("engine").GetProperty("engineCandidates").EnumerateArray().Any
                                (candidate => candidate.GetProperty("Text").GetString() == "你好")
                        );
                        if (Debug().GetProperty("totalPages").GetInt32() > 1)
                        {
                            await Click("DictionaryDebugNextPage", true);
                            await WaitDebug(state => state.GetProperty("page").GetInt32() == 2, "Scheme next page");
                            await FirstPage();
                            Check
                            (
                                scheme + "-real-page-roundtrip",
                                Debug().GetProperty("reading").GetString() == codes[index]
                            );
                        }

                        await Key(0x1B);
                        await Type("b");
                        await Key(0xBA);
                        Check
                        (
                            scheme + "-physical-semicolon",
                            Debug().GetProperty("reading").GetString() == (index is 1 or 2 ? "b;" : "b")
                        );
                        await OpenSettings();
                        foreach (var rule in rules)
                        {
                            await Click("Fuzzy" + rule, true);
                        }

                        await Click("应用");
                        await WaitApplied(1023);
                        await Click("取消");
                        Check
                        (
                            scheme + "-ten-rules-applied",
                            Debug().GetProperty("reading").GetString() == (index is 1 or 2 ? "b;" : "b")
                        );
                        await Close(ui);
                        ui = Start();
                        await Task.Delay(5000);
                        await Click("DebugOutput", true);
                        await WaitApplied(1023);
                        await Type(codes[index]);
                        Check
                        (
                            scheme + "-reopened",
                            Debug().GetProperty("engine").GetProperty("engineCandidates").EnumerateArray().Any
                                (candidate => candidate.GetProperty("Text").GetString() == "你好")
                        );
                        await Key(0x1B);
                        await OpenSettings();
                        foreach (var rule in rules)
                        {
                            await Click("Fuzzy" + rule, true);
                        }

                        await Click("应用");
                        await WaitApplied(0);
                        await Click("取消");
                        File.AppendAllText
                        (
                            Path.Combine(outputDirectory, "phases.jsonl"),
                            JsonSerializer.Serialize(new { scheme, stage = "end" }) + Environment.NewLine
                        );
                    }

                    await Close(ui);
                    File.WriteAllText(Path.Combine(outputDirectory, "report.json"), JsonSerializer.Serialize(results));
                    return results.All
                        (result => JsonSerializer.SerializeToElement(result).GetProperty("passed").GetBoolean())
                        ? 0
                        : 1;
                }

                if (minimal)
                {
                    await Type("zhang");
                    for (var repeat = 0; repeat < 10; repeat++)
                    {
                        await Click("DictionaryDebugNextPage", true);
                        await WaitDebug(state => state.GetProperty("page").GetInt32() == 2, "Minimal next " + repeat);
                        await Click("DictionaryDebugPreviousPage", true);
                        await WaitDebug
                            (state => state.GetProperty("page").GetInt32() == 1, "Minimal previous " + repeat);
                        results.Add(new { check = "page-roundtrip-" + repeat, passed = true });
                    }

                    await OpenSettings();
                    foreach (var rule in rules)
                    {
                        for (var repeat = 0; repeat < 2; repeat++)
                        {
                            await Click("Fuzzy" + rule, true);
                            results.Add(new { check = "checkbox-" + rule + "-" + repeat, passed = true });
                        }
                    }

                    await Click("取消");
                    await Close(ui);
                    File.WriteAllText(Path.Combine(outputDirectory, "report.json"), JsonSerializer.Serialize(results));
                    return 0;
                }

                await Type("zhang");
                await Capture("zhang", "zhang", "张");
                await Key(0x0D);
                var output = Debug().GetProperty("output").GetString();
                await Type("zan");
                await Key(0x25);
                await Capture("before", "zan", caret: 2, output: output);
                await OpenSettings();
                foreach (var rule in rules)
                {
                    await Click("Fuzzy" + rule, true);
                }

                File.WriteAllText(Path.Combine(outputDirectory, "all-rules.json"), ReadBounds(bounds));
                using (var allRules = JsonDocument.Parse(ReadBounds(bounds)))
                {
                    results.Add
                    (
                        new
                        {
                            check = "ten-rule-draft",
                            passed = allRules.RootElement.GetProperty("settings")[0].GetProperty("draft").GetProperty
                                ("FuzzyRuleMask").GetInt32() == 1023
                        }
                    );
                }

                await Click("取消");
                await Capture("all-rules-cancelled", "zan", caret: 2, output: output);
                await OpenSettings();
                await Click("FuzzyZhZ", true);
                await Click("FuzzyAngAn", true);
                await Click("应用");
                await WaitApplied(5);
                await Click("取消");
                await Capture("applied", "zan", "张", 2, output);
                await Key(0x1B);
                await Type("zang");
                await Capture("zang", "zang", "张");
                await Key(0x1B);
                await Type("zuanshen");
                await Capture("multi-syllable", "zuanshen", "转身");
                await OpenSettings();
                await Click("FuzzyZhZ", true);
                await Click("取消");
                await Capture("cancelled", "zuanshen", "转身");
                await Close(ui);
            }
            {
                var reopened = Start();
                await Task.Delay(5000);
                await Click("DebugOutput", true);
                await Type("zan");
                await Capture("reopened", "zan", "张");
                await OpenSettings();
                await Click("恢复默认");
                await Click("应用");
                await WaitApplied(0);
                await Click("取消");
                await Capture("defaults", "zan", caret: 3);
                results.Add
                (
                    new
                    {
                        check = "defaults-mask",
                        passed = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(settings)).GetProperty
                            ("fuzzyRuleMask").GetInt32() == 0
                    }
                );
                await Close(reopened);
            }
        }
        catch (Exception exception) when (exception is TimeoutException or InvalidOperationException
                                              or OperationCanceledException or KeyNotFoundException)
        {
            File.WriteAllText(Path.Combine(outputDirectory, "failure.txt"), exception.ToString());
            if (File.Exists(bounds))
            {
                File.Copy(bounds, Path.Combine(outputDirectory, "failure-bounds.json"), true);
            }

            results.Add(new { check = "execution", passed = false });
            if (active is not null && !active.HasExited)
            {
                active.Refresh();
                var handle = active.MainWindowHandle;
                var response = SendMessageTimeout(handle, 0, 0, 0, 2, 2000, out var responseValue);
                CaptureDesktop(Path.Combine(outputDirectory, "observer-failure-before-click.bmp"));
                using var last = JsonDocument.Parse(ReadBounds(bounds, requireFresh: false));
                var button = last.RootElement.GetProperty("controls").EnumerateArray().FirstOrDefault
                (control =>
                    control.GetProperty("name").GetString() == "DictionaryDebugSettings"
                );
                var clicked = button.ValueKind != JsonValueKind.Undefined && response != 0;
                if (clicked)
                {
                    SetForegroundWindow(handle);
                    SetCursorPos(button.GetProperty("x").GetInt32(), button.GetProperty("y").GetInt32());
                    mouse_event(2, 0, 0, 0, 0);
                    mouse_event(4, 0, 0, 0, 0);
                    await Task.Delay(1000);
                }

                CaptureDesktop(Path.Combine(outputDirectory, "observer-failure-after-click.bmp"));
                File.WriteAllText
                (
                    Path.Combine(outputDirectory, "independent-window-response.json"), JsonSerializer.Serialize
                    (
                        new
                        {
                            pid = active.Id, handle = (long)handle, wmNullResponded = response != 0,
                            responseValue = (long)responseValue, settingsClickSent = clicked,
                            businessCandidateAssertion = false
                        }
                    )
                );
                await Close(active);
            }

            if (diagnosticStderr is not null)
            {
                await File.WriteAllTextAsync
                    (Path.Combine(outputDirectory, "product-stderr.log"), await diagnosticStderr);
            }
        }

        File.WriteAllText(Path.Combine(outputDirectory, "report.json"), JsonSerializer.Serialize(results));
        return results.All(r => JsonSerializer.SerializeToElement(r).GetProperty("passed").GetBoolean()) ? 0 : 1;
    }

    private static string ReadBounds(string path, bool requireFresh = true)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        while (true)
        {
            timeout.Token.ThrowIfCancellationRequested();
            try
            {
                // The producer replaces complete snapshots; readers must allow deletion of the old file.
                using var stream = new FileStream
                    (path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream);
                var content = reader.ReadToEnd();
                if (File.Exists(path + ".error.txt"))
                {
                    throw new InvalidOperationException
                        ("Diagnostic snapshot publication failed: " + File.ReadAllText(path + ".error.txt"));
                }

                using var snapshot = JsonDocument.Parse(content);
                if (requireFresh &&
                    DateTimeOffset.UtcNow - snapshot.RootElement.GetProperty("time").GetDateTimeOffset() >
                    TimeSpan.FromSeconds(2))
                {
                    if (timeout.Token.WaitHandle.WaitOne(25))
                    {
                        throw new InvalidOperationException
                        (
                            "Diagnostic snapshot remained stale throughout the publication wait; this is not a business assertion failure"
                        );
                    }

                    continue;
                }

                _ = snapshot.RootElement.GetProperty("generation").GetInt64();
                return content;
            }
            catch (IOException exception) when ((exception.HResult & 0xffff) is 2 or 32 or 33)
            {
                if (timeout.Token.WaitHandle.WaitOne(25))
                {
                    timeout.Token.ThrowIfCancellationRequested();
                }
            }
        }
    }

    private static void CaptureDesktop(string path)
    {
        var width = GetSystemMetrics(0);
        var height = GetSystemMetrics(1);
        var screen = GetDC(0);
        var dc = CreateCompatibleDC(screen);
        var bitmap = CreateCompatibleBitmap(screen, width, height);
        var previous = SelectObject(dc, bitmap);
        try
        {
            if (!BitBlt(dc, 0, 0, width, height, screen, 0, 0, 0x00CC0020))
            {
                throw new InvalidOperationException("Desktop capture failed");
            }

            SelectObject(dc, previous);
            var header = new BitmapHeader { Size = 40, Width = width, Height = -height, Planes = 1, BitCount = 32 };
            var pixels = new byte[checked(width * height * 4)];
            if (GetDIBits(dc, bitmap, 0, (uint)height, pixels, ref header, 0) != height)
            {
                throw new InvalidOperationException("Desktop pixels unavailable");
            }

            using var writer = new BinaryWriter(File.Create(path));
            writer.Write((ushort)0x4D42);
            writer.Write(54 + pixels.Length);
            writer.Write(0);
            writer.Write(54);
            writer.Write(40);
            writer.Write(width);
            writer.Write(-height);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write(0);
            writer.Write(pixels.Length);
            writer.Write(new byte[16]);
            writer.Write(pixels);
        }
        finally
        {
            SelectObject(dc, previous);
            DeleteObject(bitmap);
            DeleteDC(dc);
            ReleaseDC(0, screen);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapHeader
    {
        internal uint Size;
        internal int Width, Height;
        internal ushort Planes, BitCount;
        internal uint Compression, SizeImage;
        internal int XPelsPerMeter, YPelsPerMeter;
        internal uint ColorsUsed, ColorsImportant;
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll")]
    private static extern nint GetDC(nint window);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(nint window, nint dc);

    [DllImport("gdi32.dll")]
    private static extern nint CreateCompatibleDC(nint dc);

    [DllImport("gdi32.dll")]
    private static extern nint CreateCompatibleBitmap(nint dc, int width, int height);

    [DllImport("gdi32.dll")]
    private static extern nint SelectObject(nint dc, nint value);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(nint value);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(nint dc);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt
        (nint target, int x, int y, int width, int height, nint source, int sx, int sy, uint operation);

    [DllImport("gdi32.dll")]
    private static extern int GetDIBits
        (nint dc, nint bitmap, uint start, uint lines, byte[] pixels, ref BitmapHeader header, uint usage);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint LoadKeyboardLayout(string layout, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SendMessageTimeout
        (nint window, uint message, nuint wparam, nint lparam, uint flags, uint timeout, out nuint result);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint window, uint message, nuint wparam, nint lparam);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(nint window);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint window, int command);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint window);

    [DllImport("user32.dll")]
    private static extern bool SetProcessDPIAware();

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, nuint extra);

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    private struct NativeInput
    {
        [FieldOffset(0)] internal uint Type;
        [FieldOffset(8)] internal KeyboardInput Keyboard;
        [FieldOffset(8)] internal MouseInput Mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        internal int X;
        internal int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        internal int X;
        internal int Y;
        internal uint Data;
        internal uint Flags;
        internal uint Time;
        internal nuint ExtraInfo;
    }

    [DllImport("user32.dll")]
    private static extern nint WindowFromPoint(NativePoint point);

    [DllImport("user32.dll")]
    private static extern nint GetAncestor(nint window, uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        internal ushort VirtualKey;
        internal ushort ScanCode;
        internal uint Flags;
        internal uint Time;
        internal nuint ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, NativeInput[] inputs, int size);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint window, out uint processId);
}