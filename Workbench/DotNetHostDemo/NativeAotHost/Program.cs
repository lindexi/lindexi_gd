string baseDirectory = AppContext.BaseDirectory;
return HostFxrRunner.Run(
    Path.Combine(baseDirectory, "ManagedComponent.Alpha.runtimeconfig.json"),
    Path.Combine(baseDirectory, "ManagedComponent.Alpha.dll"),
    "ManagedComponent.Alpha.Component, ManagedComponent.Alpha",
    "Run",
    "ManagedComponent.Alpha.ComponentEntryPoint, ManagedComponent.Alpha",
    args);
