namespace LightTextEditorPlus.Core.Tests;

[TestClass]
public static class TextTestManager
{
    [AssemblyInitialize]
    public static void GlobalInitialize(TestContext testContext)
    {
#if DEBUG
        TextEditorCore.SetAllInDebugMode();
#endif
    }

    [AssemblyCleanup]
    public static void GlobalCleanup()
    {
    }
}