using RLMatrix.Toolkit;

namespace PatternMatchingExample;

[RLMatrixEnvironment]
public partial class PatternMatchingEnvironment
{
    public int Choice => aiChoice;

    private int pattern = 0;
    private int aiChoice = 0;
    private bool roundFinished = false;

    // Simple counters for last 50 steps
    private int correct = 0;
    private int total = 0;

    // Simple accuracy calculation
    public float RecentAccuracy => total > 0 ? (float) correct / total * 100 : 0;

    [RLMatrixObservation]
    public float SeePattern() => pattern;

    [RLMatrixObservation]
    public float SeePattern2() => _pattern2;
    private int _pattern2;

    [RLMatrixActionDiscrete(10)]
    public void MakeChoice(int choice)
    {
        aiChoice = choice;
        roundFinished = true;

        // Update counters
        total++;
        if (IsRight()) correct++;
    }

    [RLMatrixReward]
    public float GiveReward() => IsRight() ? 1.0f : -1.0f;

    private bool IsRight() => aiChoice == pattern + _pattern2;

    [RLMatrixDone]
    public bool IsRoundOver() => roundFinished;

    [RLMatrixReset]
    public void StartNewRound()
    {
        if (!_manualMode)
        {
            pattern = Random.Shared.Next(3);
            _pattern2 = Random.Shared.Next(3);
            aiChoice = 0;
        }

        roundFinished = false;
    }

    public void EnterManualMode(int a,int b)
    {
        _manualMode = true;
        aiChoice = 0;

        pattern = a;
        _pattern2 = b;
    }

    private bool _manualMode = false;

    public void ResetStats()
    {
        correct = 0;
        total = 0;
    }
}