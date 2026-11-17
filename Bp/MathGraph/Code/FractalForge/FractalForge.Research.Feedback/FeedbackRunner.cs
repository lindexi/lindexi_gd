using System.Globalization;
using System.Text.Json;
using FractalForge.Training;
using MathGraphs.Neural;

namespace FractalForge.Research.Feedback;

/// <summary>Feedback 独立入口；运行命令使用共享事务，prepare 只准备基线与配置、不训练。</summary>
public static class FeedbackRunner
{
    /// <summary>prepare templatePath configPath seed Local|CrossLayer delay weight；其他命令遵循 ForgeRunner。</summary>
    public static async Task<int> RunAsync(IReadOnlyList<string> arguments, TextWriter output, TextWriter error,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (arguments.Count >= 1 && arguments[0] == "prepare")
            {
                if (arguments.Count != 8 || !int.TryParse(arguments[3], out var seed) ||
                    !Enum.TryParse<FeedbackTopology>(arguments[4], out var topology) ||
                    !int.TryParse(arguments[5], out var delay) ||
                    !double.TryParse(arguments[6], NumberStyles.Float, CultureInfo.InvariantCulture, out var weight) || arguments[7] != "F1")
                    throw new ArgumentException(FeedbackExperiment.Message("PrepareUsage"));
                var template = await ForgeRunConfiguration.LoadAsync(arguments[1], cancellationToken).ConfigureAwait(false);
                var action = FeedbackExperiment.CreateAction(topology, delay, weight);
                var configPath = Path.GetFullPath(arguments[2]);
                var sourcePath = configPath + ".forge";
                if (File.Exists(configPath) || File.Exists(sourcePath)) throw new IOException(FeedbackExperiment.Message("AlreadyExists"));
                var tokenizer = await MiniMindTextTokenizer.LoadAsync(template.TokenizerPath, template.TokenizerConfigPath,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                var bindings = new TokenBindings(tokenizer.Identity, ["I"], ["O"],
                    Enumerable.Range(0, tokenizer.VocabularySize).Select(i => (IReadOnlyList<double>)new[] { (double)i / tokenizer.VocabularySize }).ToArray(), seed: seed);
                var model = FeedbackExperiment.CreateBaseline(bindings, seed);
                _ = model.Apply(action, cancellationToken);
                var archive = ForgeArchive.Export(model);
                var configuration = template with { Direction = "Feedback", Semantics = ForgeRunner.Semantics,
                    SourceArchive = sourcePath, SourceSha256 = ForgeArchive.Fingerprint(archive), TokenizerIdentity = tokenizer.Identity, Action = action };
                FeedbackExperiment.ValidateConfiguration(configuration);
                await using (var source = new FileStream(sourcePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    await source.WriteAsync(archive, cancellationToken).ConfigureAwait(false);
                await using (var target = new FileStream(configPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    await JsonSerializer.SerializeAsync(target, configuration, cancellationToken: cancellationToken).ConfigureAwait(false);
                await output.WriteLineAsync(JsonSerializer.Serialize(new { Configuration = configPath, Source = sourcePath, configuration.SourceSha256,
                    StateRule = "F1-full-zero-reset", BaselineFeedback = "O->I delay=1", action })).ConfigureAwait(false);
                return 0;
            }
            if (arguments.Count >= 2 && arguments[0] is "train" or "resume" or "inspect" or "evaluate" or "generate")
                FeedbackExperiment.ValidateConfiguration(await ForgeRunConfiguration.LoadAsync(arguments[1], cancellationToken).ConfigureAwait(false));
            await output.WriteLineAsync(FeedbackExperiment.Message("ProtocolReport")).ConfigureAwait(false);
            return await ForgeRunner.RunAsync("Feedback", arguments, output, error, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await error.WriteLineAsync(FeedbackExperiment.Message("Cancelled")).ConfigureAwait(false);
            return 130;
        }
        catch (ArgumentException exception)
        {
            await error.WriteLineAsync(exception.Message).ConfigureAwait(false);
            return 2;
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or NotSupportedException or JsonException or UnauthorizedAccessException)
        {
            await error.WriteLineAsync(FeedbackExperiment.Message("Failed") + " " + exception.Message).ConfigureAwait(false);
            return 1;
        }
    }
}
