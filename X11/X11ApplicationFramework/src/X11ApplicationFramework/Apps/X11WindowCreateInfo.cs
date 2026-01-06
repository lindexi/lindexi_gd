namespace X11ApplicationFramework.Apps;

public readonly record struct X11WindowCreateInfo
{
    public X11WindowCreateInfo()
    {
    }

    public bool IsFullScreen { get; init; } = true;

    public int Width { get; init; } = 1280;
    public int Height { get; init; } = 720;

    ///// <summary>
    ///// 这个属性存在时，啥都不做
    ///// </summary>
    //public IntPtr? X11WindowIntPtr { get; set; }
}