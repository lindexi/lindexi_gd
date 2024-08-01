//using BujeeberehemnaNurgacolarje;

//using ReewheaberekaiNayweelehe;

//using SkiaInkCore.Primitive;

//namespace SkiaInkCore.Interactives;

//enum InputMode
//{
//    Ink,
//    Manipulate,
//}

//class InkingInputManager
//{
//    public InkingInputManager(SkInkCanvas skInkCanvas)
//    {
//        SkInkCanvas = skInkCanvas;
//        var testInput = new TestInput(skInkCanvas);
//        testInput.RenderSplashScreen();
//    }

//    public SkInkCanvas SkInkCanvas { get; }

//    public InputMode InputMode { set; get; } = InputMode.Manipulate;

//    private int _downCount;

//    private StylusPoint _lastStylusPoint;
//    private StylusPoint _firstStylusPoint;
//    private int MainInput { get; set; }

//    public void Down(InkingModeInputArgs args)
//    {
//        _downCount++;

//        if (_downCount == 1)
//        {
//            _firstStylusPoint = args.StylusPoint;
//            MainInput = args.Id;
//        }

//        if (args.Id == MainInput)
//        {
//            _lastStylusPoint = args.StylusPoint;
//        }

//        if (InputMode == InputMode.Ink)
//        {
//            SkInkCanvas.DrawStrokeDown(args);
//        }
//        else if (InputMode == InputMode.Manipulate)
//        {
//            if (args.Id == MainInput)
//            {
//                SkInkCanvas.ManipulateMoveStart(args.StylusPoint.Point);
//            }
//        }
//    }

//    public void Move(InkingModeInputArgs args)
//    {
//        if (InputMode == InputMode.Ink)
//        {
//            SkInkCanvas.DrawStrokeMove(args);
//        }
//        else if (InputMode == InputMode.Manipulate)
//        {
//            if (_downCount == 1)
//            {
//                SkInkCanvas.ManipulateMove(args.StylusPoint.Point);
//            }
//            else
//            {
//                if (args.Id != MainInput)
//                {
//                    return;
//                }

//                var x = (float) (args.StylusPoint.Point.X - _lastStylusPoint.Point.X);
//                var y = (float) (args.StylusPoint.Point.Y - _lastStylusPoint.Point.Y);

//                x = 1 + x / 100;
//                y = 1 + y / 100;

//                x = MathF.Max(0.1f, MathF.Min(10, x));
//                y = MathF.Max(0.1f, MathF.Min(10, y));

//                SkInkCanvas.ManipulateScale(new ScaleContext(x, y, (float) _firstStylusPoint.Point.X, (float) _firstStylusPoint.Point.Y));
//            }

//            _lastStylusPoint = args.StylusPoint;
//        }
//    }

//    public void Up(InkingModeInputArgs args)
//    {
//        _downCount--;
//        if (InputMode == InputMode.Ink)
//        {
//            SkInkCanvas.DrawStrokeUp(args);
//        }
//        else if (InputMode == InputMode.Manipulate)
//        {
//            if (args.Id != MainInput)
//            {
//                return;
//            }

//            SkInkCanvas.ManipulateMove(args.StylusPoint.Point);
//            SkInkCanvas.ManipulateFinish();

//            _lastStylusPoint = args.StylusPoint;
//        }
//    }
//}