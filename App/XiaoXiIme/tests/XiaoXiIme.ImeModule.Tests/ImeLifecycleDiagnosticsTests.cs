using System.Runtime.CompilerServices;
using XiaoXiIme.ImeModule;

namespace XiaoXiIme.ImeModule.Tests;

[CollectionDefinition("Lifecycle environment", DisableParallelization = true)]
public class LifecycleEnvironmentCollection
{
}

[Collection("Lifecycle environment")]
public class ImeLifecycleDiagnosticsTests
{
    [Theory]
    [InlineData(null, 0)]
    [InlineData(null, 1)]
    [InlineData("", 0)]
    [InlineData("   ", 1)]
    [InlineData("C:\\invalid|ime-log.txt", 0)]
    [InlineData("C:\\invalid|ime-log.txt", 1)]
    public unsafe void ImeSetActiveContext_WhenLogDisabledOrInvalidThenSucceeds(string? path, int active)
    {
        var previous = Environment.GetEnvironmentVariable("XIAOXIIME_LIFECYCLE_LOG");
        try
        {
            Environment.SetEnvironmentVariable("XIAOXIIME_LIFECYCLE_LOG", path);
            delegate* unmanaged[Stdcall]<nint, int, int> export = &ImeExports.ImeSetActiveContext;

            Assert.Equal(1, export(0, active));
        }
        finally
        {
            Environment.SetEnvironmentVariable("XIAOXIIME_LIFECYCLE_LOG", previous);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("C:\\invalid|ime-log.txt")]
    public unsafe void ImeSelect_WhenLogDisabledOrInvalidThenSucceeds(string? path)
    {
        var previous = Environment.GetEnvironmentVariable("XIAOXIIME_LIFECYCLE_LOG");
        try
        {
            Environment.SetEnvironmentVariable("XIAOXIIME_LIFECYCLE_LOG", path);
            delegate* unmanaged[Stdcall]<nint, int, int> export = &ImeExports.ImeSelect;

            Assert.Equal(1, export(0, 1));
        }
        finally
        {
            Environment.SetEnvironmentVariable("XIAOXIIME_LIFECYCLE_LOG", previous);
        }
    }
}
