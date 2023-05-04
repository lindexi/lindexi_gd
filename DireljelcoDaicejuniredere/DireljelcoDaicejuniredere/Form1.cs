using System.Runtime.InteropServices;

namespace DireljelcoDaicejuniredere;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();

        Load += Form1_Load;
    }

    private void Form1_Load(object? sender, EventArgs e)
    {
        // 使用 WinForm 项目只是为了方便获取到窗口而已
        var handle = Handle;

        Guid clsid = new Guid("{DECBDC16-E824-436e-872D-14E8C7BF7D8B}");
        Guid iid = new Guid("{C6C77F97-545E-4873-85F2-E0FEE550B2E9}");
        string licenseKey = "{CAAD7274-4004-44e0-8A17-D6F1919C443A}";
        m_nativeIRTS = (IRealTimeStylusNative) ComObjectCreator.CreateInstanceLicense(clsid, iid, licenseKey);

        m_nativeIRTS.SetHWND(handle);

        m_nativeIRTS.GetHWND(out var hWnd);


        var useMouseForInput = true;

        m_nativeIRTS.SetAllTabletsMode(useMouseForInput);

        m_addCursorShim = new StylusSyncPluginNativeShim();
        IntPtr interfaceForObject1 = Marshal.GetComInterfaceForObject((object) m_addCursorShim, typeof(StylusSyncPluginNative));

        m_nativeIRTS.AddStylusSyncPlugin(0U, interfaceForObject1);

        m_nativeIRTS.MultiTouchEnable(true);
        m_nativeIRTS.Enable(true);
    }

    private IRealTimeStylusNative m_nativeIRTS;
    private StylusSyncPluginNativeShim m_addCursorShim;
}