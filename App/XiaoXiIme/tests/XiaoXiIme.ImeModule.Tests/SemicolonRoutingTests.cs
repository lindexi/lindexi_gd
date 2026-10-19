using System.Runtime.CompilerServices;
using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeCore;
using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule.Tests;

[Collection("Lifecycle environment")]
public class SemicolonRoutingTests
{
    [Theory]
    [InlineData("sogouDoublePinyin", true)]
    [InlineData("microsoftDoublePinyin", true)]
    [InlineData("fullPinyin", false)]
    [InlineData("xiaoheDoublePinyin", false)]
    [InlineData("abcDoublePinyin", false)]
    [InlineData("jiajiaDoublePinyin", false)]
    [InlineData("naturalCodeDoublePinyin", false)]
    public unsafe void WhenNativeEntryReceivesSemicolonThenOnlyTargetSchemeClaimsIt(string scheme, bool expected)
    {
        var context = new ImeContext(new InMemoryImeDictionary(new ImeLexicon(
            [new("冰", "bing", 1)], [], ImeProjectionDefinition.ForScheme(scheme), 1)));
        context.ProcessKey(ImeKey.FromCharacter('b'));
        ImeModuleRuntime.SetSnapshotForTesting(ImeSessionId.Default, context.Snapshot);
        delegate* unmanaged[Stdcall]<nint, uint, nint, byte*, int> entry = &ImeExports.ImeProcessKey;
        try
        {
            Assert.Equal(expected ? 1 : 0, entry(0, 0xBA, 0, null));
        }
        finally
        {
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    public static IEnumerable<object[]> ModeCases()
    {
        foreach (var scheme in new[] { "sogouDoublePinyin", "microsoftDoublePinyin", "fullPinyin", "xiaoheDoublePinyin", "abcDoublePinyin", "jiajiaDoublePinyin", "naturalCodeDoublePinyin" })
        foreach (var open in new[] { false, true })
        foreach (var native in new[] { false, true })
        foreach (var shifted in new[] { false, true })
            yield return [scheme, open, native, shifted];
    }

    [Theory]
    [MemberData(nameof(ModeCases))]
    public unsafe void WhenSessionsHaveDifferentSchemesThenOnlyOpenChineseTargetClaimsSemicolon(string scheme, bool open, bool native, bool shifted)
    {
        var target = new HImc(0x11);
        var other = new HImc(0x22);
        var memory = new InputContext { FOpen = open ? 1 : 0, FdwConversion = native ? ImeConstants.ImeCmodeNative : 0 };
        var accessor = new InMemoryImmContextAccessor((nint)(&memory), 0, 0);
        var context = new ImeContext(new InMemoryImeDictionary(new ImeLexicon([], [], ImeProjectionDefinition.ForScheme(scheme), 1)));
        var opposite = new ImeContext(new InMemoryImeDictionary(new ImeLexicon([], [],
            ImeProjectionDefinition.ForScheme(scheme is "sogouDoublePinyin" or "microsoftDoublePinyin" ? "fullPinyin" : "sogouDoublePinyin"), 1)));
        ImeModuleRuntime.SetSnapshotForTesting(ImeSessionId.FromHimc(other.Value), opposite.Snapshot);
        ImeModuleRuntime.SetSnapshotForTesting(ImeSessionId.FromHimc(target.Value), context.Snapshot);
        ImeModuleRuntime.SetCompositionContextReaderForTesting(new(accessor));
        try
        {
            Assert.Equal(open && native && !shifted && scheme is ("sogouDoublePinyin" or "microsoftDoublePinyin"),
                ImeModuleRuntime.ShouldProcessVirtualKey(0xBA, shifted ? ImeKeyTranslator.ShiftModifier : 0, target));
        }
        finally
        {
            ImeModuleRuntime.SetCompositionContextReaderForTesting(null);
            ImeModuleRuntime.SetBridgeForTesting(null);
        }
    }

    [Fact]
    public void WhenNoSchemeIsKnownThenTranslatorDoesNotClaimSemicolon()
    {
        Assert.False(ImeKeyTranslator.ShouldProcess(0xBA));
    }
}
