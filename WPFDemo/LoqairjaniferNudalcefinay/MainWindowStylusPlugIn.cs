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

    protected override void OnStylusDown(RawStylusInput rawStylusInput)
    {
        int id = rawStylusInput.StylusDeviceId;

        if (_dictionary.TryGetValue(id, out var info))
        {
            _currentWarnMessage = $"重复 Down Id={id}";
        }

        _dictionary[id] = new TouchInfo(rawStylusInput);
        LogMessage();

        base.OnStylusDown(rawStylusInput);
    }

    protected override void OnStylusMove(RawStylusInput rawStylusInput)
    {
        int id = rawStylusInput.StylusDeviceId;

        if (!_dictionary.TryGetValue(id, out var info))
        {
            _currentWarnMessage = $"未找到 Move Id={id}";
            _dictionary[id] = new TouchInfo(rawStylusInput);
        }
        else
        {
            _dictionary[id] = new TouchInfo(rawStylusInput);
        }
        LogMessage();

        base.OnStylusMove(rawStylusInput);
    }

    protected override void OnStylusUp(RawStylusInput rawStylusInput)
    {
        int id = rawStylusInput.StylusDeviceId;

        if (!_dictionary.TryGetValue(id, out var info))
        {
            _currentWarnMessage = $"未找到 Up Id={id}";
        }
        else
        {
            _dictionary.Remove(id);
        }
        LogMessage();

        base.OnStylusUp(rawStylusInput);
    }

    private void LogMessage()
    {
        var message =string.Empty;
        if (!string.IsNullOrEmpty(_currentWarnMessage))
        {
            message += $"Warning: {_currentWarnMessage}" + Environment.NewLine;
        }
        message += $"Touch Count: {_dictionary.Count}" + Environment.NewLine;
        foreach (var item in _dictionary.Values)
        {
            message += $"Id={item.Id}, X={item.X:0.000}, Y={item.Y:0.000}" + Environment.NewLine;
        }

        _mainWindow.LogMessage(message.TrimEnd());
    }
}

record TouchInfo
{
    public TouchInfo(RawStylusInput rawStylusInput)
    {
        Id = rawStylusInput.StylusDeviceId;
        var collection = rawStylusInput.GetStylusPoints();
        if (collection.Count>0)
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

    public int Id { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
}