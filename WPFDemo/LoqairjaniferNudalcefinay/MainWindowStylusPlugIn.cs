using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;

namespace LoqairjaniferNudalcefinay;

class MainWindowStylusPlugIn : StylusPlugIn
{
    public MainWindowStylusPlugIn(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
    }

    private readonly MainWindow _mainWindow;

    private readonly Dictionary<int, TouchInfo> _dictionary = [];

    private string? _currentWarnMessage;
    private string? _message;

    protected override void OnStylusDown(RawStylusInput rawStylusInput)
    {
        int id = rawStylusInput.StylusDeviceId;
        Debug.WriteLine($"Down {id}");

        //Thread.Sleep(15);

        if (_dictionary.TryGetValue(id, out var info))
        {
            if (info.IsUp)
            {
                _currentWarnMessage = $"[{Thread.CurrentThread.Name}({Thread.CurrentThread.ManagedThreadId})] 先 Up 后 Down 的情况 Id={id} 移动距离{info.MoveCount}";
            }
            else
            {
                _currentWarnMessage = $"[{Thread.CurrentThread.Name}({Thread.CurrentThread.ManagedThreadId})] 重复 Down Id={id} 移动距离{info.MoveCount}";
            }
            AddError(id, _currentWarnMessage);
        }

        _dictionary[id] = new TouchInfo(rawStylusInput);
        LogMessage();

        base.OnStylusDown(rawStylusInput);
    }

    protected override void OnStylusMove(RawStylusInput rawStylusInput)
    {
        int id = rawStylusInput.StylusDeviceId;

        //Thread.Sleep(15);
        if (!_dictionary.TryGetValue(id, out var info))
        {
            //_currentWarnMessage = $"未找到 Move Id={id}";
            //Debug.WriteLine(_currentWarnMessage);
        }
        else
        {
            _dictionary[id] = new TouchInfo(rawStylusInput)
            {
                MoveCount = info.MoveCount + 1
            };
        }
        LogMessage();

        base.OnStylusMove(rawStylusInput);
    }

    protected override void OnStylusUp(RawStylusInput rawStylusInput)
    {
        int id = rawStylusInput.StylusDeviceId;

        //Thread.Sleep(15);
        if (id == 0)
        {
            // 鼠标
            return;
        }

        Debug.WriteLine($"Up {id}");

        if (!_dictionary.TryGetValue(id, out var info))
        {
            _currentWarnMessage = $"[{Thread.CurrentThread.Name}({Thread.CurrentThread.ManagedThreadId})] 未找到 Up Id={id}";
            AddError(id, _currentWarnMessage);
            _dictionary.Add(id, new TouchInfo(rawStylusInput)
            {
                IsUp = true
            });
        }
        else
        {
            _dictionary.Remove(id);
        }
        LogMessage();

        base.OnStylusUp(rawStylusInput);
    }

    private void AddError(int id, string message)
    {
        if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
        {
            _message = $"\r\n[{Thread.CurrentThread.Name}({Thread.CurrentThread.ManagedThreadId})] Id={id} Message={message} {new StackTrace()}";
        }
    }

    private void LogMessage()
    {
        var message = string.Empty;
        if (!string.IsNullOrEmpty(_currentWarnMessage))
        {
            message += $"Warning: {_currentWarnMessage}" + Environment.NewLine;
        }

        message += $"{_message}\r\n";

        message += $"Touch Count: {_dictionary.Count}" + Environment.NewLine;
        foreach (var item in _dictionary.Values)
        {
            message += $"Id={item.Id}, X={item.X:0.000}, Y={item.Y:0.000}" + Environment.NewLine;
        }
       
        _mainWindow.LogStylusPlugInMessage(message.TrimEnd());
    }
}

record TouchInfo
{
    public TouchInfo(RawStylusInput rawStylusInput)
    {
        Id = rawStylusInput.StylusDeviceId;
        var collection = rawStylusInput.GetStylusPoints();
        if (collection.Count > 0)
        {
            var point = collection[0];
            X = point.X;
            Y = point.Y;
        }
        else
        {
            X = double.NaN;
            Y = double.NaN;
        }
    }

    public TouchInfo(StylusEventArgs stylusEventArgs)
    {
        Id = stylusEventArgs.StylusDevice.Id;
        var collection = stylusEventArgs.GetStylusPoints(null);
        if (collection.Count > 0)
        {
            var point = collection[0];
            X = point.X;
            Y = point.Y;
        }
        else
        {
            X = double.NaN;
            Y = double.NaN;
        }
    }

    public TouchInfo(int id, double x, double y)
    {
        Id = id;
        X = x;
        Y = y;
    }

    public int Id { get; init; }
    public double X { get; init; }
    public double Y { get; init; }

    public int MoveCount { get; init; } = 0;

    public bool IsUp { get; init; }

    public Point AsPoint() => new Point(X, Y);
}