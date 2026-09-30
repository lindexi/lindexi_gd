using XiaoXiIme.TsfModule;

namespace XiaoXiIme.TsfModule.Tests;

public unsafe class TsfClassFactoryTests
{
    [Fact]
    public void TryCreate_UnsupportedInterfaceClearsResult()
    {
        nint result = 42;

        var hr = TsfClassFactory.TryCreate(Guid.NewGuid(), &result);

        Assert.Equal(TsfHResults.E_NOINTERFACE, hr);
        Assert.Equal(0, result);
    }

    [Fact]
    public void ClassFactory_QueryInterfaceMaintainsIdentityAndReferenceCount()
    {
        var initialObjects = TsfComServer.ObjectCount;
        nint instance = 0;
        var hr = TsfClassFactory.TryCreate(TsfClassFactory.IClassFactoryId, &instance);
        Assert.Equal(TsfHResults.S_OK, hr);
        Assert.NotEqual(0, instance);
        Assert.Equal(initialObjects + 1, TsfComServer.ObjectCount);

        var vtable = *(void***) instance;
        var queryInterface = (delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>) vtable[0];
        var release = (delegate* unmanaged[Stdcall]<nint, uint>) vtable[2];
        var unknownId = TsfClassFactory.IUnknownId;
        nint unknown = 0;

        Assert.Equal(TsfHResults.S_OK, queryInterface(instance, &unknownId, &unknown));
        Assert.Equal(instance, unknown);
        Assert.Equal(1u, release(unknown));
        Assert.Equal(0u, release(instance));
        Assert.Equal(initialObjects, TsfComServer.ObjectCount);
    }

    [Fact]
    public void ClassFactory_CreateInstanceRejectsAggregationAndUnsupportedCoclass()
    {
        nint instance = 0;
        Assert.Equal(TsfHResults.S_OK, TsfClassFactory.TryCreate(TsfClassFactory.IClassFactoryId, &instance));
        var vtable = *(void***) instance;
        var createInstance = (delegate* unmanaged[Stdcall]<nint, nint, Guid*, nint*, int>) vtable[3];
        var release = (delegate* unmanaged[Stdcall]<nint, uint>) vtable[2];
        var interfaceId = TsfClassFactory.IUnknownId;
        nint result = 42;

        Assert.Equal(TsfHResults.CLASS_E_NOAGGREGATION, createInstance(instance, 1, &interfaceId, &result));
        Assert.Equal(0, result);

        result = 42;
        Assert.Equal(TsfHResults.E_NOINTERFACE, createInstance(instance, 0, &interfaceId, &result));
        Assert.Equal(0, result);
        Assert.Equal(0u, release(instance));
    }

    [Fact]
    public void ClassFactory_LockServerBalancesAndDoesNotUnderflow()
    {
        nint instance = 0;
        Assert.Equal(TsfHResults.S_OK, TsfClassFactory.TryCreate(TsfClassFactory.IClassFactoryId, &instance));
        var vtable = *(void***) instance;
        var lockServer = (delegate* unmanaged[Stdcall]<nint, int, int>) vtable[4];
        var release = (delegate* unmanaged[Stdcall]<nint, uint>) vtable[2];
        var initialLocks = TsfComServer.LockCount;

        Assert.Equal(TsfHResults.S_OK, lockServer(instance, 1));
        Assert.Equal(initialLocks + 1, TsfComServer.LockCount);
        Assert.Equal(TsfHResults.S_OK, lockServer(instance, 0));
        Assert.Equal(initialLocks, TsfComServer.LockCount);
        Assert.Equal(TsfHResults.S_OK, lockServer(instance, 0));
        Assert.Equal(initialLocks, TsfComServer.LockCount);
        Assert.Equal(0u, release(instance));
    }

    [Fact]
    public void ServerCannotUnloadNativeAotLibrary()
    {
        Assert.False(TsfComServer.CanUnload);
    }
}