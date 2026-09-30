using XiaoXiIme.TsfModule;

namespace XiaoXiIme.TsfModule.Tests;

public unsafe class TsfEditSessionTests
{
    [Fact]
    public void EditSession_ImplementsComIdentityAndExecutes()
    {
        var initialObjects = TsfComServer.ObjectCount;
        nint instance = 0;

        Assert.Equal(TsfHResults.S_OK, TsfEditSession.TryCreate(&instance));
        Assert.NotEqual(0, instance);
        Assert.Equal(initialObjects + 1, TsfComServer.ObjectCount);

        var vtable = *(void***) instance;
        var queryInterface = (delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>) vtable[0];
        var release = (delegate* unmanaged[Stdcall]<nint, uint>) vtable[2];
        var doEditSession = (delegate* unmanaged[Stdcall]<nint, uint, int>) vtable[3];
        var editSessionId = TsfNative.IEditSessionId;
        nint queried = 0;

        Assert.Equal(TsfHResults.S_OK, queryInterface(instance, &editSessionId, &queried));
        Assert.Equal(instance, queried);
        Assert.Equal(TsfHResults.S_OK, doEditSession(instance, 42));
        Assert.Equal(1u, release(queried));
        Assert.Equal(0u, release(instance));
        Assert.Equal(initialObjects, TsfComServer.ObjectCount);
    }

    [Fact]
    public void EditSession_QueryInterfaceRejectsUnsupportedInterfaceAndClearsResult()
    {
        nint instance = 0;
        Assert.Equal(TsfHResults.S_OK, TsfEditSession.TryCreate(&instance));
        var vtable = *(void***) instance;
        var queryInterface = (delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>) vtable[0];
        var release = (delegate* unmanaged[Stdcall]<nint, uint>) vtable[2];
        var unsupportedId = Guid.NewGuid();
        nint result = 42;

        Assert.Equal(TsfHResults.E_NOINTERFACE, queryInterface(instance, &unsupportedId, &result));
        Assert.Equal(0, result);
        Assert.Equal(0u, release(instance));
    }
}
