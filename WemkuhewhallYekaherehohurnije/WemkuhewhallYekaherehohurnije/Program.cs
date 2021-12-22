using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace WemkuhewhallYekaherehohurnije
{
    class Program
    {
        private static IWindow _window;

        private static void Main(string[] args)
        {
            //Create a window.
            var options = WindowOptions.Default;

            options = new WindowOptions
            (
                true,
                new Vector2D<int>(50, 50),
                new Vector2D<int>(1280, 720),
                0.0,
                0.0,
                new GraphicsAPI
                (
                    ContextAPI.OpenGL,
                    ContextProfile.Core,
                    ContextFlags.ForwardCompatible,
                    new APIVersion(3, 3)
                ),
                "",
                WindowState.Normal,
                WindowBorder.Resizable,
                false,
                false,
                VideoMode.Default
            );

            options.Size = new Vector2D<int>(800, 600);
            options.Title = "LearnOpenGL with Silk.NET";

            _window = Window.Create(options);

            //Assign events.
            _window.Load += OnLoad;
            _window.Update += OnUpdate;
            _window.Render += OnRender;

            //Run the window.
            _window.Run();
        }

        private static void OnLoad()
        {
            //Set-up input context. 
            IInputContext input = _window.CreateInput();
            for (int i = 0; i < input.Keyboards.Count; i++)
            {
                input.Keyboards[i].KeyDown += KeyDown;
            }
        }

        private static void OnRender(double obj)
        {
            //Here all rendering should be done.
        }

        private static void OnUpdate(double obj)
        {
            //Here all updates to the program should be done.
        }

        private static void KeyDown(IKeyboard arg1, Key arg2, int arg3)
        {
            //Check to close the window on escape.
            if (arg2 == Key.Escape)
            {
                _window.Close();
            }
        }
    }
}