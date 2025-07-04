// Set up how our AI will learn

using System.Diagnostics;
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

        //Console.WriteLine("Press Enter to continue...");
        //Console.ReadLine();
    }
}

Console.WriteLine("Training complete!");

for (int i = 0; i < 1000; i++)
{
    Console.WriteLine($"Input a and b");
    var line = Console.ReadLine();

    if (line == null)
    {
        continue;
    }

    var split = line.Split(' ');
    if (split.Length != 2)
    {
        continue;
    }

    if (int.TryParse(split[0], out var a) && int.TryParse(split[1], out var b))
    {
        environment.EnterManualMode(a, b);

        await agent.Step(isTraining: false);

        var excepted = a + b;
        Debug.Assert(a == environment.SeePattern());
        Debug.Assert(b == environment.SeePattern2());

        Console.WriteLine($"Choice={environment.Choice} Excepted={excepted} Right={environment.Choice == excepted}");
    }
}

Console.ReadLine();