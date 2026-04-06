namespace VolcEngineSdk;

public class ArkContentGeneration(ArkClient arkClient)
{
    public ArkClient Client => arkClient;

    public ArkContentGenerationTasks Tasks => field ??= new ArkContentGenerationTasks(arkClient);
}