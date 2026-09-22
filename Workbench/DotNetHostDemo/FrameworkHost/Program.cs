string baseDirectory = AppContext.BaseDirectory;
int exitCode = HostFxrRunner.Run(
    Path.Combine(baseDirectory, "ManagedComponent.Alpha.runtimeconfig.json"),
    Path.Combine(baseDirectory, "ManagedComponent.Alpha.dll"),
    "ManagedComponent.Alpha.Component, ManagedComponent.Alpha",
    "Run",
    "ManagedComponent.Alpha.ComponentEntryPoint, ManagedComponent.Alpha",
    Array.Empty<string>());

Environment.Exit(exitCode);
return exitCode;
