using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;

namespace NaeawurcaDowhemcenunalfe;

internal class Program
{
    static void Main(string[] args)
    {
        var mainWindow = new MainWindow(new GameWindowSettings()
        {
            RenderFrequency = 0,
        },new NativeWindowSettings()
        {
            Size = new Vector2i(1000,1000/2),
            Title = "OpenTK",
            Vsync = VSyncMode.Off
        });
        mainWindow.Run();
    }
}

public sealed class MainWindow : GameWindow
{
    public MainWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
    {
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        Title = $"(Vsync: {VSync}) FPS: {1f / args.Time:0}";

        Color4 backColor;
        backColor.A = 1.0f;
        backColor.R = Random.Shared.NextSingle();
        backColor.G = Random.Shared.NextSingle();
        backColor.B = Random.Shared.NextSingle();
        GL.ClearColor(backColor);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        SwapBuffers();
    }
}