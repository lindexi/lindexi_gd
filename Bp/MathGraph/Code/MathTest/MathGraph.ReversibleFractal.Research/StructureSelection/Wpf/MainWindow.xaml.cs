using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MathGraph.StructureSelection.Wpf;

public partial class MainWindow : Window
{
    private Study? _study;

    /// <summary>Initializes the experiment window without starting a run.</summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void RunButton_Click(object sender, RoutedEventArgs e)
    {
        RunButton.IsEnabled = false;
        TrialSelector.IsEnabled = false;
        TrialSelector.ItemsSource = null;
        Results.Visibility = Visibility.Collapsed;
        Results.DataContext = null;
        _study = null;
        Status.Text = Text("Running");

        try
        {
            var study = await Task.Run(Experiment.Run);
            _study = study;
            Protocol.Text = Format("StudyBudget", study.TrainSize, study.ValidationSize, study.TestSize,
                study.CandidateUpdates, study.BatchSize, study.LearningRate);
            TrialSelector.ItemsSource = study.Trials.Select(trial => new TrialOption(
                Format("TrialLabel", trial.Task, trial.Seed, trial.Checkpoint), trial)).ToArray();
            TrialSelector.SelectedIndex = 0;
            TrialSelector.IsEnabled = true;
            Status.Text = Format("Completed", study.Trials.Count);
        }
        catch (InvalidOperationException error)
        {
            Trace.TraceError(error.ToString());
            Status.Text = Format("Failed", error.Message);
        }
        finally
        {
            RunButton.IsEnabled = true;
        }
    }

    private void TrialSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_study is not { } study || TrialSelector.SelectedItem is not TrialOption option)
            return;

        var trial = option.Trial;
        double maximum = trial.Candidates.Max(candidate => Math.Max(candidate.ValidationMse, candidate.TestMse));
        var cards = trial.Candidates.Select(candidate => new CandidateCard(
            Text(candidate.Strategy), candidate.Strategy == trial.RetainedStrategy,
            CreateBar(candidate.ValidationMse, maximum, Brushes.RoyalBlue),
            CreateBar(candidate.TestMse, maximum, Brushes.DarkCyan), candidate.RetentionScore,
            candidate.Point is { } point && candidate.Direction is { } direction
                ? Format("Point", point, Text(direction)) : Text("NoPoint"),
            Format("Topology", candidate.Leaves, candidate.Edges, candidate.Parameters),
            Format("CandidateBudget", candidate.TrainingExamples, candidate.TrainingPortVisits))).ToArray();

        Results.DataContext = new TrialView(
            Format("Decision", Text(trial.RetainedStrategy), study.RelativeMargin, study.ExtraParameterPenalty),
            Format("PairBudget", trial.BatchFingerprint, trial.SelectionGradientExamples, trial.SelectionGradientPortVisits),
            Format("Scale", maximum), cards);
        Results.Visibility = Visibility.Visible;
    }

    private string Text(string key) => (string)FindResource(key);

    private string Format(string key, params object[] args) => string.Format(Text(key), args);

    private static ErrorBar CreateBar(double value, double maximum, Brush color)
    {
        double fraction = maximum > 0 ? value / maximum : 0;
        return new(value, new GridLength(fraction, GridUnitType.Star),
            new GridLength(1 - fraction, GridUnitType.Star), color);
    }

    private sealed record TrialOption(string Title, Trial Trial);

    private sealed record TrialView(string Decision, string PairBudget, string Scale, CandidateCard[] Candidates);

    private sealed record CandidateCard(string Title, bool Retained, ErrorBar Validation, ErrorBar Test, double Score,
        string Selection, string Topology, string TrainingBudget);

    private sealed record ErrorBar(double Value, GridLength Filled, GridLength Remaining, Brush Color);
}
