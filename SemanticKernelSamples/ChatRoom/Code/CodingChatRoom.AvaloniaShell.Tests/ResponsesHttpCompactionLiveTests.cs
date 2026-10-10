using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
[TestCategory("LiveApi")]
[DoNotParallelize]
public sealed class ResponsesHttpCompactionLiveTests
{
    private const string KeyPath = @"C:\lindexi\Work\Key\MiniMax.txt";
    private const string Endpoint = "https://api.minimaxi.com/v1/";
    private const string ModelId = "MiniMax-M3";

    [TestMethod]
    [TestCategory("CompactionAcceptance")]
    public async Task RawHttpCompactionShouldPreserveContextWithoutSdkAsync()
    {
        if (!File.Exists(KeyPath)) return;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        using var http = new HttpClient { BaseAddress = new Uri(Endpoint) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await File.ReadAllTextAsync(KeyPath, cancellation.Token)).Trim());
        string marker = $"PROJECT-{Guid.NewGuid():N}";
        string reportPath = Path.Combine(Path.GetTempPath(), $"responses-http-compact-{Guid.NewGuid():N}.json");
        var records = new List<Exchange>();
        try
        {
            var history = new List<JsonElement>
            {
                UserMessage($"For this C# project the test identifier is {marker}. Acknowledge it briefly."),
            };
            using var first = await PostAsync("responses", new { model = ModelId, input = history, stream = false, store = false });
            history.AddRange(first.RootElement.GetProperty("output").EnumerateArray().Select(item => item.Clone()));
            JsonElement question = UserMessage(historyQuestion);
            using var baseline = await PostAsync("responses", new
            {
                model = ModelId, input = history.Append(question).ToArray(), stream = false, store = false,
            });
            string baselineAnswer = Answer(baseline.RootElement);
            Console.WriteLine($"History question: {historyQuestion}");
            Console.WriteLine($"Baseline: {baselineAnswer}");
            StringAssert.Contains(baselineAnswer, marker);

            using var compact = await PostAsync("responses/compact", new { model = ModelId, input = history });
            var compacted = compact.RootElement.GetProperty("output").EnumerateArray().Select(item => item.Clone()).ToList();
            Console.WriteLine($"Compact output: {compact.RootElement.GetProperty("output").GetRawText()}");
            using var continuation = await PostAsync("responses", new
            {
                model = ModelId, input = compacted.Append(question).ToArray(), stream = false, store = false,
            });
            string answer = Answer(continuation.RootElement);
            Console.WriteLine($"Raw HTTP continuation: {answer}");
            StringAssert.Contains(answer, marker);

            compacted.Add(question);
            compacted.AddRange(continuation.RootElement.GetProperty("output").EnumerateArray().Select(item => item.Clone()));
            compacted.Add(UserMessage("Repeat that exact project identifier once more."));
            using var second = await PostAsync("responses", new { model = ModelId, input = compacted, stream = false, store = false });
            StringAssert.Contains(Answer(second.RootElement), marker);
        }
        finally
        {
            await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(new { marker, historyQuestion, records },
                new JsonSerializerOptions { WriteIndented = true }), CancellationToken.None);
            Console.WriteLine($"Raw HTTP report: {reportPath}");
        }

        async Task<JsonDocument> PostAsync(string path, object payload)
        {
            string requestJson = JsonSerializer.Serialize(payload);
            using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            using var response = await http.PostAsync(path, content, cancellation.Token);
            string responseJson = await response.Content.ReadAsStringAsync(cancellation.Token);
            records.Add(new Exchange(new Uri(http.BaseAddress!, path).ToString(), requestJson,
                (int)response.StatusCode, responseJson));
            Assert.IsTrue(response.IsSuccessStatusCode, $"HTTP {(int)response.StatusCode}: {responseJson}");
            return JsonDocument.Parse(responseJson);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task RawHttpCompactionFactsAndContinuationShouldBeDiagnosedAsync(bool useSimpleUserMessage)
    {
        if (!File.Exists(KeyPath)) return;
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        using var http = new HttpClient { BaseAddress = new Uri(Endpoint) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await File.ReadAllTextAsync(KeyPath, timeout.Token)).Trim());
        string marker = $"PROJECT-{Guid.NewGuid():N}";
        string fileName = $"Fixture_{Guid.NewGuid():N}.cs";
        string facts = $"C# project facts: identifier={marker}; target framework=net9.0; test framework=xUnit; source file={fileName}. No files have been changed. The task is to review a null-check, not execute commands or restart anything.";
        var history = new[]
        {
            useSimpleUserMessage ? JsonSerializer.SerializeToElement(new { role = "user", content = facts }) : UserMessage(facts),
            JsonSerializer.SerializeToElement(new
            {
                type = "message", role = "assistant", content = new[]
                {
                    new { type = "output_text", text = $"Confirmed project {marker}, net9.0, xUnit, source {fileName}. No changes or commands performed.", annotations = Array.Empty<object>() },
                },
            }),
        };
        const string question = "List the project identifier, target framework, test framework, source filename, and work already performed from the supplied context. Do not invent values.";
        var exchanges = new List<Exchange>();
        var results = new List<object>();
        string report = Path.Combine(Path.GetTempPath(), $"compaction-facts-{Guid.NewGuid():N}.json");
        try
        {
            using var baseline = await PostAsync("responses", new { model = ModelId, input = history.Append(UserMessage(question)), stream = false });
            Console.WriteLine($"Facts baseline: {Answer(baseline.RootElement)}");
            StringAssert.Contains(Answer(baseline.RootElement), marker);
            foreach (bool explicitInstructions in new[] { false, true })
            {
                var payload = new Dictionary<string, object> { ["model"] = ModelId, ["input"] = history };
                if (explicitInstructions) payload.Add("instructions",
                    "Preserve only facts explicitly present in the input. Keep exact identifiers, filenames and framework versions. Do not invent tasks, files, tool calls, environment details or next steps. Record that no work has been performed.");
                using var compact = await PostAsync("responses/compact", payload);
                var output = compact.RootElement.GetProperty("output").EnumerateArray().Select(item => item.Clone()).ToArray();
                Console.WriteLine($"Explicit instructions={explicitInstructions}, compact output: {compact.RootElement.GetProperty("output").GetRawText()}");
                using var continuation = await PostAsync("responses", new
                {
                    model = ModelId, input = output.Append(UserMessage(question)), stream = false,
                });
                string answer = Answer(continuation.RootElement);
                Console.WriteLine($"Simple user message={useSimpleUserMessage}, explicit instructions={explicitInstructions}, continuation: {answer}");
                results.Add(new
                {
                    explicitInstructions,
                    Compaction = compact.RootElement.Clone(),
                    Continuation = continuation.RootElement.Clone(),
                    RecognizedIdentifier = answer.Contains(marker, StringComparison.Ordinal),
                    RecognizedFilename = answer.Contains(fileName, StringComparison.Ordinal),
                });
            }
        }
        finally
        {
            await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { marker, fileName, useSimpleUserMessage, results, exchanges },
                new JsonSerializerOptions { WriteIndented = true }), CancellationToken.None);
            Console.WriteLine($"Facts report: {report}");
        }
        Assert.HasCount(2, results);

        async Task<JsonDocument> PostAsync(string path, object payload)
        {
            string json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await http.PostAsync(path, content, timeout.Token);
            string body = await response.Content.ReadAsStringAsync(timeout.Token);
            exchanges.Add(new Exchange(new Uri(http.BaseAddress!, path).ToString(), json, (int)response.StatusCode, body));
            Assert.IsTrue(response.IsSuccessStatusCode, body);
            return JsonDocument.Parse(body);
        }
    }

    [TestMethod]
    public async Task RawHttpCompactionEncodingShouldNotChangeContinuationAsync()
    {
        if (!File.Exists(KeyPath)) return;
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var http = new HttpClient { BaseAddress = new Uri(Endpoint) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await File.ReadAllTextAsync(KeyPath, timeout.Token)).Trim());
        string marker = $"PROJECT-{Guid.NewGuid():N}";
        var records = new List<Exchange>();
        string report = Path.Combine(Path.GetTempPath(), $"compaction-encoding-{Guid.NewGuid():N}.json");
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var relaxed = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        try
        {
            using var compact = await PostAsync("responses/compact", JsonSerializer.Serialize(new
            {
                model = ModelId,
                input = new[] { UserMessage($"C# project identifier: {marker}. No work has been performed.") },
            }));
            var items = compact.RootElement.GetProperty("output").EnumerateArray().Select(item => item.Clone()).ToList();
            items.Add(UserMessage("List the project facts in the supplied context, including the identifier. Do not guess."));
            var payload = new { model = ModelId, input = items, stream = false, store = false };
            string escapedJson = JsonSerializer.Serialize(payload);
            string relaxedJson = JsonSerializer.Serialize(payload, relaxed);
            using var escapedDocument = JsonDocument.Parse(escapedJson);
            using var relaxedDocument = JsonDocument.Parse(relaxedJson);
            Assert.AreEqual(escapedDocument.RootElement.GetProperty("input")[0].GetProperty("encrypted_content").GetString(),
                relaxedDocument.RootElement.GetProperty("input")[0].GetProperty("encrypted_content").GetString());
            using var escapedResponse = await PostAsync("responses", escapedJson);
            using var relaxedResponse = await PostAsync("responses", relaxedJson);
            string escapedAnswer = Answer(escapedResponse.RootElement);
            string relaxedAnswer = Answer(relaxedResponse.RootElement);
            Console.WriteLine($"Default encoding answer: {escapedAnswer}");
            Console.WriteLine($"Relaxed encoding answer: {relaxedAnswer}");
            Console.WriteLine($"Default recognized: {escapedAnswer.Contains(marker, StringComparison.Ordinal)}; relaxed recognized: {relaxedAnswer.Contains(marker, StringComparison.Ordinal)}");
            StringAssert.Contains(relaxedAnswer, marker);
        }
        finally
        {
            await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { marker, records },
                new JsonSerializerOptions { WriteIndented = true }), CancellationToken.None);
            Console.WriteLine($"Encoding report: {report}");
        }

        async Task<JsonDocument> PostAsync(string path, string json)
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await http.PostAsync(path, content, timeout.Token);
            string body = await response.Content.ReadAsStringAsync(timeout.Token);
            records.Add(new Exchange(new Uri(http.BaseAddress!, path).ToString(), json, (int)response.StatusCode, body));
            Assert.IsTrue(response.IsSuccessStatusCode, body);
            return JsonDocument.Parse(body);
        }
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task RawHttpCompactionShouldContinueActualCodingConversationAsync(bool useSystemProxy)
    {
        if (!File.Exists(KeyPath)) return;
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        Console.WriteLine($"Use system proxy: {useSystemProxy}; default proxy bypasses endpoint: {HttpClient.DefaultProxy.IsBypassed(new Uri(Endpoint))}");
        using var http = new HttpClient(new HttpClientHandler { UseProxy = useSystemProxy }) { BaseAddress = new Uri(Endpoint) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await File.ReadAllTextAsync(KeyPath, timeout.Token)).Trim());
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var history = new List<JsonElement>();
        var records = new List<Exchange>();
        string report = Path.Combine(Path.GetTempPath(), $"compaction-coding-flow-{Guid.NewGuid():N}.json");
        const string instructions = "You are a C# code reviewer. Answer from the provided conversation. Keep answers brief.";
        try
        {
            foreach (string prompt in new[]
            {
                "Review this C# method: string Normalize(string value) => value.Trim(); Target framework is net9.0 and tests use xUnit. Explain the null handling issue, do not use tools.",
                "The requirement is to throw ArgumentNullException for null input, return trimmed text otherwise. Propose the implementation, do not use tools.",
                "One more requirement: an empty string must remain empty. Summarize the requirements and proposed tests, do not use tools.",
            })
            {
                history.Add(UserMessage(prompt));
                using var response = await PostAsync("responses", new { model = ModelId, instructions, input = history, stream = false });
                history.AddRange(response.RootElement.GetProperty("output").EnumerateArray().Select(item => item.Clone()));
            }
            var question = UserMessage("Using the prior review, what method were we reviewing, which exception should it throw for null, and which test framework are we using? Return these three facts only.");
            using var baseline = await PostAsync("responses", new { model = ModelId, instructions, input = history.Append(question), stream = false });
            Console.WriteLine($"Coding baseline: {Answer(baseline.RootElement)}");
            StringAssert.Contains(Answer(baseline.RootElement), "ArgumentNullException");
            using var compact = await PostAsync("responses/compact", new { model = ModelId, input = history });
            Console.WriteLine($"Coding compaction: {compact.RootElement.GetProperty("output").GetRawText()}");
            var output = compact.RootElement.GetProperty("output").EnumerateArray().Select(item => item.Clone()).ToArray();
            using var continuation = await PostAsync("responses", new { model = ModelId, instructions, input = output.Append(question), stream = false });
            string answer = Answer(continuation.RootElement);
            Console.WriteLine($"Coding continuation: {answer}");
            string streamingJson = JsonSerializer.Serialize(new { model = ModelId, instructions, input = output.Append(question), stream = true });
            using var streamingContent = new StringContent(streamingJson, Encoding.UTF8, "application/json");
            using var streamingResponse = await http.PostAsync("responses", streamingContent, timeout.Token);
            string sse = await streamingResponse.Content.ReadAsStringAsync(timeout.Token);
            records.Add(new Exchange(new Uri(http.BaseAddress!, "responses").ToString(), streamingJson, (int)streamingResponse.StatusCode, sse));
            Assert.IsTrue(streamingResponse.IsSuccessStatusCode, sse);
            var streamedText = new StringBuilder();
            foreach (string line in sse.Split('\n').Where(line => line.StartsWith("data: ", StringComparison.Ordinal)))
            {
                string data = line[6..].Trim();
                if (data == "[DONE]") continue;
                using var update = JsonDocument.Parse(data);
                if (update.RootElement.GetProperty("type").GetString() == "response.output_text.delta")
                    streamedText.Append(update.RootElement.GetProperty("delta").GetString());
            }
            Console.WriteLine($"Coding streaming continuation: {streamedText}");
            StringAssert.Contains(answer, "ArgumentNullException");
            StringAssert.Contains(answer, "Normalize");
            StringAssert.Contains(answer, "xUnit");
        }
        finally
        {
            await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { records },
                new JsonSerializerOptions { WriteIndented = true }), CancellationToken.None);
            Console.WriteLine($"Coding flow report: {report}");
        }

        async Task<JsonDocument> PostAsync(string path, object payload)
        {
            string json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await http.PostAsync(path, content, timeout.Token);
            string body = await response.Content.ReadAsStringAsync(timeout.Token);
            records.Add(new Exchange(new Uri(http.BaseAddress!, path).ToString(), json, (int)response.StatusCode, body));
            Assert.IsTrue(response.IsSuccessStatusCode, body);
            return JsonDocument.Parse(body);
        }
    }

    [TestMethod]
    public async Task RawHttpStoredInputItemsShouldRetainCompactionAsync()
    {
        if (!File.Exists(KeyPath)) return;
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var http = new HttpClient { BaseAddress = new Uri(Endpoint) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await File.ReadAllTextAsync(KeyPath, timeout.Token)).Trim());
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        string marker = $"PROJECT-{Guid.NewGuid():N}";
        var records = new List<Exchange>();
        string report = Path.Combine(Path.GetTempPath(), $"compaction-stored-inputs-{Guid.NewGuid():N}.json");
        try
        {
            using var compactBody = new StringContent(JsonSerializer.Serialize(new
            {
                model = ModelId, input = new[] { UserMessage($"C# project identifier: {marker}.") },
            }), Encoding.UTF8, "application/json");
            using var compactResponse = await http.PostAsync("responses/compact", compactBody, timeout.Token);
            string compactText = await compactResponse.Content.ReadAsStringAsync(timeout.Token);
            Assert.IsTrue(compactResponse.IsSuccessStatusCode, compactText);
            using var compact = JsonDocument.Parse(compactText);
            var input = compact.RootElement.GetProperty("output").EnumerateArray().Select(item => item.Clone()).ToList();
            input.Add(UserMessage("What is the project identifier from the context?"));
            string json = JsonSerializer.Serialize(new { model = ModelId, input, store = true, stream = false });
            using var body = new StringContent(json, Encoding.UTF8, "application/json");
            using var continuationResponse = await http.PostAsync("responses", body, timeout.Token);
            string continuationText = await continuationResponse.Content.ReadAsStringAsync(timeout.Token);
            records.Add(new Exchange(new Uri(http.BaseAddress!, "responses").ToString(), json,
                (int)continuationResponse.StatusCode, continuationText));
            Assert.IsTrue(continuationResponse.IsSuccessStatusCode, continuationText);
            using var continuation = JsonDocument.Parse(continuationText);
            string id = continuation.RootElement.GetProperty("id").GetString() ?? throw new InvalidDataException("Response ID missing.");
            Console.WriteLine($"Continuation: {Answer(continuation.RootElement)}");
            Console.WriteLine($"Returned store: {continuation.RootElement.GetProperty("store").GetRawText()}");
            foreach (string path in new[] { $"responses/{Uri.EscapeDataString(id)}", $"responses/{Uri.EscapeDataString(id)}/input_items" })
            {
                using var response = await http.GetAsync(path, timeout.Token);
                string text = await response.Content.ReadAsStringAsync(timeout.Token);
                records.Add(new Exchange(new Uri(http.BaseAddress!, path).ToString(), string.Empty, (int)response.StatusCode, text));
                Console.WriteLine($"GET {path}: HTTP {(int)response.StatusCode}; body={text}");
            }
        }
        finally
        {
            await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { marker, records },
                new JsonSerializerOptions { WriteIndented = true }), CancellationToken.None);
            Console.WriteLine($"Stored inputs report: {report}");
        }
    }

    [TestMethod]
    public async Task RawHttpRetainedUserMessagesShouldNotMaskMissingAssistantContextAsync()
    {
        if (!File.Exists(KeyPath)) return;
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        using var http = new HttpClient { BaseAddress = new Uri(Endpoint) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await File.ReadAllTextAsync(KeyPath, timeout.Token)).Trim());
        string marker = $"ASSISTANT-{Guid.NewGuid():N}";
        var user = UserMessage("Choose a unique C# test fixture identifier and record your choice. We will use that choice later.");
        var assistant = JsonSerializer.SerializeToElement(new
        {
            type = "message", role = "assistant", content = new[]
            {
                new { type = "output_text", text = $"My chosen C# test fixture identifier is {marker}.", annotations = Array.Empty<object>() },
            },
        });
        var question = UserMessage("What exact C# test fixture identifier did the assistant choose earlier? Do not choose a new value.");
        var records = new List<Exchange>();
        var findings = new List<object>();
        string report = Path.Combine(Path.GetTempPath(), $"compaction-retained-users-{Guid.NewGuid():N}.json");
        try
        {
            using var baseline = await PostAsync("responses", new { model = ModelId, input = new[] { user, assistant, question }, stream = false });
            StringAssert.Contains(Answer(baseline.RootElement), marker);
            using var compact = await PostAsync("responses/compact", new { model = ModelId, input = new[] { user, assistant } });
            var output = compact.RootElement.GetProperty("output").EnumerateArray().Select(item => item.Clone()).ToArray();
            Console.WriteLine($"Assistant fact present in compact JSON: {compact.RootElement.GetProperty("output").GetRawText().Contains(marker, StringComparison.Ordinal)}");
            foreach (string variant in new[] { "native-output", "retained-user-and-native", "retained-user-only" })
            {
                var input = new List<JsonElement>();
                if (variant != "native-output") input.Add(user);
                if (variant != "retained-user-only") input.AddRange(output);
                input.Add(question);
                using var response = await PostAsync("responses", new { model = ModelId, input, stream = false });
                string answer = Answer(response.RootElement);
                bool recognized = answer.Contains(marker, StringComparison.Ordinal);
                findings.Add(new { variant, recognized, answer });
                Console.WriteLine($"Variant={variant}, recognized={recognized}, answer={answer}");
            }
        }
        finally
        {
            await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { marker, findings, records },
                new JsonSerializerOptions { WriteIndented = true }), CancellationToken.None);
            Console.WriteLine($"Retained users report: {report}");
        }
        Assert.HasCount(3, findings);

        async Task<JsonDocument> PostAsync(string path, object payload)
        {
            string json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await http.PostAsync(path, content, timeout.Token);
            string body = await response.Content.ReadAsStringAsync(timeout.Token);
            records.Add(new Exchange(new Uri(http.BaseAddress!, path).ToString(), json, (int)response.StatusCode, body));
            Assert.IsTrue(response.IsSuccessStatusCode, body);
            return JsonDocument.Parse(body);
        }
    }

    [TestMethod]
    public async Task RawHttpCompactionInputValidationShouldBeDiagnosedAsync()
    {
        if (!File.Exists(KeyPath)) return;
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        using var http = new HttpClient { BaseAddress = new Uri(Endpoint) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await File.ReadAllTextAsync(KeyPath, timeout.Token)).Trim());
        string marker = $"PROJECT-{Guid.NewGuid():N}";
        var records = new List<Exchange>();
        var findings = new List<object>();
        string report = Path.Combine(Path.GetTempPath(), $"compaction-validation-{Guid.NewGuid():N}.json");
        try
        {
            var compactExchange = await PostAsync("responses/compact", new
            {
                model = ModelId, input = new[] { UserMessage($"C# project identifier: {marker}.") },
            });
            Assert.IsTrue(compactExchange.Status is >= 200 and < 300, compactExchange.Response);
            using var compact = JsonDocument.Parse(compactExchange.Response);
            var output = compact.RootElement.GetProperty("output").EnumerateArray().Select(item => item.Clone()).ToArray();
            var native = output.Single(item => item.GetProperty("type").GetString() == "compaction");
            var variants = new[]
            {
                (Name: "normal-message", Item: UserMessage($"C# project identifier: {marker}.")),
                (Name: "native-compaction", Item: native),
                (Name: "unknown-type", Item: JsonSerializer.SerializeToElement(new
                {
                    type = "diagnostic_unknown_item", encrypted_content = native.GetProperty("encrypted_content").GetString(),
                })),
                (Name: "missing-content", Item: JsonSerializer.SerializeToElement(new { type = "compaction" })),
                (Name: "numeric-content", Item: JsonSerializer.SerializeToElement(new { type = "compaction", encrypted_content = 123 })),
            };
            foreach (var variant in variants)
            {
                var exchange = await PostAsync("responses", new
                {
                    model = ModelId, stream = false,
                    input = new[] { variant.Item, UserMessage("Repeat the C# project identifier from the supplied context exactly. Do not invent a value.") },
                });
                string answer = exchange.Response;
                int? tokens = null;
                if (exchange.Status is >= 200 and < 300)
                {
                    using var response = JsonDocument.Parse(exchange.Response);
                    answer = Answer(response.RootElement);
                    if (response.RootElement.TryGetProperty("usage", out var usage))
                        tokens = usage.GetProperty("input_tokens").GetInt32();
                }
                bool recognized = answer.Contains(marker, StringComparison.Ordinal);
                findings.Add(new { variant.Name, exchange.Status, tokens, recognized, answer });
                Console.WriteLine($"Validation variant={variant.Name}, HTTP={exchange.Status}, tokens={tokens}, recognized={recognized}, response={answer}");
                if (variant.Name == "normal-message") Assert.IsTrue(recognized, answer);
            }
        }
        finally
        {
            await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { marker, findings, records },
                new JsonSerializerOptions { WriteIndented = true }), CancellationToken.None);
            Console.WriteLine($"Validation report: {report}");
        }
        Assert.HasCount(5, findings);

        async Task<Exchange> PostAsync(string path, object payload)
        {
            string json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await http.PostAsync(path, content, timeout.Token);
            string body = await response.Content.ReadAsStringAsync(timeout.Token);
            var exchange = new Exchange(new Uri(http.BaseAddress!, path).ToString(), json, (int)response.StatusCode, body);
            records.Add(exchange);
            return exchange;
        }
    }

    private static JsonElement UserMessage(string text) => JsonSerializer.SerializeToElement(new
    {
        type = "message", role = "user", content = new[] { new { type = "input_text", text } },
    });

    private static string Answer(JsonElement response) => string.Join("", response.GetProperty("output").EnumerateArray()
        .Where(item => item.GetProperty("type").GetString() == "message")
        .SelectMany(item => item.GetProperty("content").EnumerateArray())
        .Where(part => part.GetProperty("type").GetString() == "output_text")
        .Select(part => part.GetProperty("text").GetString()));

    private sealed record Exchange(string Url, string Request, int Status, string Response);
}
