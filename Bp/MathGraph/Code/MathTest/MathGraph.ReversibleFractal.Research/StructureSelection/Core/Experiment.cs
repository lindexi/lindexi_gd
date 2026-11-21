namespace MathGraph.StructureSelection;

/// <summary>A candidate's topology, matched update budget, cost and held-out errors.</summary>
public sealed record Candidate(string Strategy, string? Point, string? Direction, double? TrainingSignal,
    int Leaves, int Edges, int Parameters, int Updates, int TrainingExamples, int TrainingPortVisits,
    double InitialValidationMse, double InitialPredictionDrift, double ValidationMse,
    double RetentionScore, double TestMse);

/// <summary>One paired look from the unexpanded training trajectory.</summary>
public sealed record Trial(string Task, int Seed, int Checkpoint, string BatchFingerprint, double CheckpointTrainingMse,
    int SelectionGradientExamples, int SelectionGradientPortVisits, int LegalLeaves, int ForwardLeaves,
    int ReverseLeaves, bool SamePoint, string RetainedStrategy, IReadOnlyList<Candidate> Candidates);

/// <summary>The first accepted expansion, if any, in one task and seed trajectory.</summary>
public sealed record FirstAcceptance(string Task, int Seed, int? Checkpoint, string? Point, string? Strategy);

/// <summary>The fixed protocol and all paired results; no test error is used to select a candidate.</summary>
public sealed record Study(int TrainSize, int ValidationSize, int TestSize, int BatchSize, int CandidateUpdates,
    double LearningRate, double RelativeMargin, double ExtraParameterPenalty, IReadOnlyList<Trial> Trials,
    IReadOnlyList<FirstAcceptance> FirstAcceptances);

/// <summary>Deterministic, single-substitution structure search with a no-growth comparator.</summary>
public static class Experiment
{
    private const int TrainSize = 128;
    private const int ValidationSize = 256;
    private const int TestSize = 1024;
    private const int BatchSize = 16;
    private const int Updates = 64;
    private const double Rate = 0.025;
    private const double RelativeMargin = 0.01;
    private const double ParameterPenalty = 0.001;
    private static readonly int[] Checkpoints = [0, 64, 256];
    private static readonly int[] Seeds = [11, 23, 37, 53, 71, 89, 107, 127];
    private static readonly string[] Tasks = ["affine", "mixed"];

    /// <summary>Runs all fixed tasks, seeds and checkpoint budgets without reading external data.</summary>
    public static Study Run()
    {
        var trials = new List<Trial>();
        var first = new List<FirstAcceptance>();
        foreach (string task in Tasks)
        foreach (int seed in Seeds)
        {
            var train = Data(task, TrainSize, Stream(seed, 1));
            var validation = Data(task, ValidationSize, Stream(seed, 2));
            var test = Data(task, TestSize, Stream(seed, 3));
            var parent = new Network(FivePointGraph.Create());
            var init = Stream(seed, 4);
            for (int i = 0; i < parent.Weights.Length; i++) parent.Weights[i] = (init.Uniform() - 0.5) * 0.8;
            var warmup = Stream(seed, 5);
            int previous = 0;
            FirstAcceptance? accepted = null;
            foreach (int checkpoint in Checkpoints)
            {
                Train(parent, train, Batches(warmup, checkpoint - previous));
                previous = checkpoint;
                var trial = Compare(task, seed, checkpoint, parent, train, validation, test);
                var winner = trial.Candidates.Single(c => c.Strategy == trial.RetainedStrategy);
                if (winner.Point is not null) accepted ??= new(task, seed, checkpoint, winner.Point, winner.Strategy);
                trials.Add(trial);
            }
            first.Add(accepted ?? new(task, seed, null, null, null));
        }
        return new(TrainSize, ValidationSize, TestSize, BatchSize, Updates, Rate, RelativeMargin, ParameterPenalty,
            trials.AsReadOnly(), first.AsReadOnly());
    }

    /// <summary>Retains only an expansion that clears both the validation margin and its capacity penalty.</summary>
    public static double Score(double baselineMse, double candidateMse, int extraParameters)
        => (baselineMse - candidateMse) / Math.Max(baselineMse, 1e-12)
            - RelativeMargin - ParameterPenalty * extraParameters;

    private static Trial Compare(string task, int seed, int checkpoint, Network parent,
        Sample[] train, Sample[] validation, Sample[] test)
    {
        var gradient = new double[parent.Weights.Length];
        double trainMse = parent.Measure(train, [], gradient);
        var scores = parent.Scores(gradient);
        var legal = parent.Graph.Leaves;
        string random = legal[Stream(seed, 1000 + checkpoint).Index(legal.Count)].Id;
        string signal = legal.OrderByDescending(l => scores[l.Id]).ThenBy(l => l.Id, StringComparer.Ordinal).First().Id;
        var batch = Batches(Stream(seed, 2000 + checkpoint), Updates);
        var reference = validation.Select(s => parent.Evaluate(s.X0, s.X1)).ToArray();
        var candidates = new List<(Candidate Result, Network Model)>();
        foreach (var (strategy, point) in new (string, string?)[] { ("continue", null), ("random", random), ("signal", signal) })
        {
            var model = parent.Fork(point);
            double start = model.Measure(validation, []);
            double drift = validation.Select((s, i) => Math.Abs(model.Evaluate(s.X0, s.X1) - reference[i])).Max();
            Train(model, train, batch);
            double mse = model.Measure(validation, []);
            double score = point is null ? 0 : Score(candidates[0].Result.ValidationMse, mse,
                model.Weights.Length - parent.Weights.Length);
            string? direction = point is null ? null : legal.Single(l => l.Id == point).Inputs == 2 ? "forward" : "reverse";
            candidates.Add((new Candidate(strategy, point, direction, point is null ? null : scores[point],
                model.Graph.Leaves.Count, model.Graph.Edges.Count, model.Weights.Length, Updates, batch.Length,
                batch.Length * model.UnitsPerSample * 2, start, drift, mse, score, 0), model));
        }
        string retained = candidates.OrderByDescending(c => c.Result.RetentionScore).First().Result.Strategy;
        return new(task, seed, checkpoint, Fingerprint(batch), trainMse, train.Length, train.Length * parent.UnitsPerSample * 2,
            legal.Count, legal.Count(l => l.Inputs == 2), legal.Count(l => l.Inputs == 1), random == signal, retained,
            Array.AsReadOnly(candidates.Select(c => c.Result with { TestMse = c.Model.Measure(test, []) }).ToArray()));
    }

    private static void Train(Network model, Sample[] data, int[] batches)
    {
        var gradient = new double[model.Weights.Length];
        for (int i = 0; i < batches.Length; i += BatchSize)
        {
            model.Measure(data, batches.AsSpan(i, BatchSize), gradient);
            for (int p = 0; p < gradient.Length; p++) model.Weights[p] -= Rate * gradient[p];
        }
    }

    private static int[] Batches(RandomStream rng, int steps)
        => Enumerable.Range(0, steps * BatchSize).Select(_ => rng.Index(TrainSize)).ToArray();

    private static string Fingerprint(int[] batches)
    {
        ulong hash = 14695981039346656037UL;
        foreach (int i in batches) hash = unchecked((hash ^ (uint)i) * 1099511628211UL);
        return hash.ToString("X16");
    }

    private static Sample[] Data(string task, int count, RandomStream rng)
        => Enumerable.Range(0, count).Select(_ =>
        {
            double x0 = 2 * rng.Uniform() - 1;
            double x1 = 2 * rng.Uniform() - 1;
            double linear = 0.25 + 0.65 * x0 - 0.35 * x1;
            double y = task == "affine" ? linear : linear + 0.65 * Math.Tanh(3 * x0 * x1)
                + 0.4 * Math.Tanh(4 * x0 - 2 * x1);
            return new Sample(x0, x1, y);
        }).ToArray();

    private static RandomStream Stream(int seed, int domain) => new(((ulong)seed << 32) | (uint)domain);

    private sealed class RandomStream(ulong state)
    {
        private ulong Next()
        {
            unchecked
            {
                ulong z = state += 0x9E3779B97F4A7C15UL;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        internal double Uniform() => (Next() >> 11) * (1.0 / (1UL << 53));

        internal int Index(int count)
        {
            ulong bound = (ulong)count;
            ulong threshold = unchecked(0UL - bound) % bound;
            ulong n;
            do n = Next(); while (n < threshold);
            return (int)(n % bound);
        }
    }
}
