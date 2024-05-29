namespace UnoInk.X11Platforms;

public struct X11WindowCreateInfo
{
    public X11WindowCreateInfo()
    {
    }
    
    public bool IsFullScreen { get; set; } = true;
    
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 720;
    
    ///// <summary>
    ///// 这个属性存在时，啥都不做
    ///// </summary>
    //public IntPtr? X11WindowIntPtr { get; set; }
}
