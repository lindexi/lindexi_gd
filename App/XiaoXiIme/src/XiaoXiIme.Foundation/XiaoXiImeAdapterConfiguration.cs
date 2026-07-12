namespace XiaoXiIme.Foundation;

public static class XiaoXiImeAdapterConfiguration
{
    private static ImeAdapterKind s_defaultAdapter = ImeAdapterKind.Imm32;

    public static ImeAdapterKind DefaultAdapter
    {
        get => s_defaultAdapter;
        set
        {
            if (!Enum.IsDefined(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported IME adapter kind.");
            }

            s_defaultAdapter = value;
        }
    }
}