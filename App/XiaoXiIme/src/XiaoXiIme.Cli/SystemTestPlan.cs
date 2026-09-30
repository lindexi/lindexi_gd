using System.Text.Json;

namespace XiaoXiIme.Cli;

internal sealed record SystemTestStep(
    string Id,
    string Area,
    string Description,
    bool Destructive,
    string Evidence);

internal sealed record SystemTestPlan(
    string Name,
    IReadOnlyList<SystemTestStep> Steps)
{
    public static SystemTestPlan CreateDefault() => new(
        "XiaoXiIme Windows system validation",
        [
            new("build", "Build", "Build the full solution and all test hosts in Release.", false, "build.binlog and console log"),
            new("unit-tests", "Tests", "Run core, dictionary, IPC, traditional IME, TSF, UI and integration tests.", false, "TRX test results"),
            new("publish-ime", "Traditional IME", "Publish the NativeAOT traditional IME module and verify required exports.", false, "published binary and export list"),
            new("publish-tsf", "TSF", "Publish the NativeAOT TSF module and run ABI/vtable validation.", false, "ABI host log"),
            new("com-activation", "TSF", "Run isolated per-user CLSID registration and CoGetClassObject validation.", true, "COM activation log and registry cleanup result"),
            new("host-ipc", "Host/IPC", "Start the host and validate IPC health, request routing and shutdown.", false, "host and IPC logs"),
            new("install", "Installation", "Install the selected traditional IME and TSF profiles on a disposable VM.", true, "registry exports and installer logs"),
            new("input-switch", "Windows integration", "Switch between system input methods and verify activation/deactivation.", true, "screen recording and event log"),
            new("text-input", "End-to-end", "Exercise composition, candidates, commit, focus switching and application shutdown.", true, "captured text, screenshots and crash dumps"),
            new("cleanup", "Rollback", "Unregister profiles, unload layouts, stop host processes and verify no test registry entries remain.", true, "cleanup report and registry diff"),
        ]);

    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
}
