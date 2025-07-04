// Set up how our AI will learn
using PatternMatchingExample;

using RLMatrix;
using RLMatrix.Agents.Common;

var learningSetup = new DQNAgentOptions(
    batchSize: 32,      // Learn from 32 experiences at once
    memorySize: 1000,   // Remember last 1000 attempts
    gamma: 0.99f,       // Care a lot about future rewards
    epsStart: 1f,       // Start by trying everything
    epsEnd: 0.05f,      // Eventually stick to what works
    epsDecay: 150f      // How fast to transition
);

// Create our environment
var environment = new PatternMatchingEnvironment().RLInit();
var env = new List<IEnvironmentAsync<float[]>> {
    environment,
    //new PatternMatchingEnvironment().RLInit() //you can add more than one to train in parallel
};

// Create our learning agent
var agent = new LocalDiscreteRolloutAgent<float[]>(learningSetup, env);

// Let it learn!
for (int i = 0; i < 1000; i++)
{
    await agent.Step();

    if ((i + 1) % 50 == 0)
    {
        Console.WriteLine($"Step {i + 1}/1000 - Last 50 steps accuracy: {environment.RecentAccuracy:F1}%");
        environment.ResetStats();

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }
}

Console.WriteLine("Training complete!");
Console.ReadLine();