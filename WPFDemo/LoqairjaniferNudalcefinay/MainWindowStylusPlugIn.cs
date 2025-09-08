using System.Windows.Input.StylusPlugIns;

namespace LoqairjaniferNudalcefinay;

class MainWindowStylusPlugIn : StylusPlugIn
{
    public MainWindowStylusPlugIn(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
    }

    private readonly MainWindow _mainWindow;

    

    protected override void OnStylusDown(RawStylusInput rawStylusInput)
    {
        base.OnStylusDown(rawStylusInput);
    }

    protected override void OnStylusMove(RawStylusInput rawStylusInput)
    {
        base.OnStylusMove(rawStylusInput);
    }

    protected override void OnStylusUp(RawStylusInput rawStylusInput)
    {
        base.OnStylusUp(rawStylusInput);
    }
}