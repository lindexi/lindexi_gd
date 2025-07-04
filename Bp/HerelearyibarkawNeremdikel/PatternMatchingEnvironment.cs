using RLMatrix.Toolkit;

namespace PatternMatchingExample;

[RLMatrixEnvironment]
public partial class PatternMatchingEnvironment
{
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

    [RLMatrixActionDiscrete(2)]
    public void MakeChoice(int choice)
    {
        aiChoice = choice;
        roundFinished = true;

        // Update counters
        total++;
        if (aiChoice == pattern) correct++;
    }

    [RLMatrixReward]
    public float GiveReward() => aiChoice == pattern ? 1.0f : -1.0f;

    [RLMatrixDone]
    public bool IsRoundOver() => roundFinished;

    [RLMatrixReset]
    public void StartNewRound()
    {
        pattern = Random.Shared.Next(2);
        aiChoice = 0;
        roundFinished = false;
    }

    public void ResetStats()
    {
        correct = 0;
        total = 0;
    }
}