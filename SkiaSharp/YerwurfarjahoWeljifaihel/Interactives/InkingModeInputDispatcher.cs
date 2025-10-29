using System.Diagnostics;
using SkiaInkCore.Diagnostics;

namespace SkiaInkCore.Interactives;

/// <summary>
/// 输入调度器
/// </summary>
class InkingModeInputDispatcher
{
    public InkingModeInputDispatcher()
    {
    }

    public InkingModeInputDispatcher(IInkingInputProcessor inputProcessor):this()
    {
        AddInputProcessor(inputProcessor);
    }

    private HashSet<int> CurrentInputIdHashSet { get; } = new HashSet<int>();

    /// <summary>
    /// 首个输入点
    /// </summary>
    public int MainInputId { private set; get; }

    /// <summary>
    /// 是否输入开始了
    /// </summary>
    public bool IsInputStart { private set; get; }

    /// <summary>
    /// 距离输入过去多久，仅在 <see cref="IsInputStart"/> 为 true 时，才有意义
    /// </summary>
    public TimeSpan InputDuring => _inputDuringStopwatch.Elapsed;

    private readonly Stopwatch _inputDuringStopwatch = new Stopwatch();

    public void Down(in InkingModeInputArgs args)
    {
        CurrentInputIdHashSet.Add(args.Id);

        if (CurrentInputIdHashSet.Count == 1)
        {
            MainInputId = args.Id;
            ProcessInputStart();
        }

        ProcessDown(in args);
    }

    private void ProcessInputStart()
    {
        IsInputStart = true;
        _inputDuringStopwatch.Restart();

        foreach (var inputProcessor in InputProcessors)
        {
            if (inputProcessor.Enable)
            {
                inputProcessor.InputStart();
            }
        }
    }

    private void ProcessDown(in InkingModeInputArgs args)
    {
        foreach (var inputProcessor in InputProcessors)
        {
            if (inputProcessor.Enable)
            {
                inputProcessor.Down(args);
            }
        }
    }

    public void Move(in InkingModeInputArgs args)
    {
        if (CurrentInputIdHashSet.Contains(args.Id))
        {
            foreach (var inputProcessor in InputProcessors)
            {
                if (inputProcessor.Enable)
                {
                    if (inputProcessor.InputProcessorSettings.EnableMultiTouch || MainInputId == args.Id)
                    {
                        inputProcessor.Move(args);
                    }
                }
            }
        }
        else
        {
            if (args.IsMouse)
            {
                foreach (var inputProcessor in InputProcessors)
                {
                    if (inputProcessor.Enable)
                    {
                        inputProcessor.Hover(args);
                    }
                }
            }
            else
            {
                // 非鼠标没有 Hover 效果
                // 如果是在 IsInputStart=false 时，代表触摸离开之后，收到离开之后的消息
                // 对应的问题记录：手势橡皮擦进入工具条时，先触发 Leave 里面，符合预期的进行结束手势橡皮擦。然而后续居然又继续收到 Move 事件，导致判断橡皮擦逻辑工作，再次错误进入了手势橡皮擦模式
                StaticDebugLogger.WriteLine($"[{nameof(InkingModeInputDispatcher)}] Lost Move IsInputStart={IsInputStart} Id={args.Id}");
            }
        }
    }

    public void Up(InkingModeInputArgs args)
    {
        if (CurrentInputIdHashSet.Remove(args.Id))
        {
            if (args.Id == MainInputId)
            {
                StaticDebugLogger.WriteLine($"[{nameof(InkingModeInputDispatcher)}] MainIdUp MainId={MainInputId}");
            }

            foreach (var inputProcessor in InputProcessors)
            {
                if (inputProcessor.Enable)
                {
                    if (inputProcessor.InputProcessorSettings.EnableMultiTouch || MainInputId == args.Id)
                    {
                        inputProcessor.Up(args);
                    }
                }
            }

            if (CurrentInputIdHashSet.Count == 0)
            {
                ProcessInputComplete();
            }
        }
        else
        {
            // 啥都不能做
        }
    }

    private void ProcessInputComplete()
    {
        foreach (var inputProcessor in InputProcessors)
        {
            if (inputProcessor.Enable)
            {
                inputProcessor.InputComplete();
            }
        }
        IsInputStart = false;
        _inputDuringStopwatch.Stop();
    }

    /// <summary>
    /// 输入被其他拿走了，比如鼠标移动到窗口外抬起
    /// </summary>
    public void Leave()
    {
        StaticDebugLogger.WriteLine($"{nameof(InkingModeInputDispatcher)} Leave");

        foreach (var inputProcessor in InputProcessors)
        {
            if (inputProcessor.Enable)
            {
                inputProcessor.Leave();
            }
        }

        CurrentInputIdHashSet.Clear();
        IsInputStart = false;
        _inputDuringStopwatch.Stop();
    }

    /// <summary>
    /// 加上输入处理者，有输入时自然执行
    /// </summary>
    /// <param name="inputProcessor"></param>
    public void AddInputProcessor(IInkingInputProcessor inputProcessor)
    {
        InputProcessors.Add(inputProcessor);
        if (inputProcessor is IInkingModeInputDispatcherSensitive modeInputDispatcherSensitive)
        {
            modeInputDispatcherSensitive.ModeInputDispatcher = this;
        }
    }

    private List<IInkingInputProcessor> InputProcessors { get; } = new List<IInkingInputProcessor>();

    //public bool Enable => true;

    /// <summary>
    /// 某个触摸 Id 是否存在。不存在则代表被抬起
    /// </summary>
    /// <param name="deviceId"></param>
    /// <returns></returns>
    public bool ContainsDeviceId(int deviceId) => CurrentInputIdHashSet.Contains(deviceId);

    /// <summary>
    /// 当前有多少手指按下
    /// </summary>
    public int CurrentDeviceCount => CurrentInputIdHashSet.Count;
}