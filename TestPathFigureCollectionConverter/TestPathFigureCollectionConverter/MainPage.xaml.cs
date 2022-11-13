using Microsoft.Maui.Controls.Shapes;

namespace TestPathFigureCollectionConverter;

public partial class MainPage : ContentPage
{
    int count = 0;

    public MainPage()
    {
        InitializeComponent();


        Loaded += MainPage_Loaded;
    }

    private void MainPage_Loaded(object sender, EventArgs e)
    {
        var pathFigureCollectionConverter = new PathFigureCollectionConverter();
        var pathString1 =
            "M320 96a64 64 0 0 0-64 64V896a64 64 0 0 0 64 64h384a64 64 0 0 0 64-64V160a64 64 0 0 0-64-64H320z m384 64V896H320V160h384zM128 256v576h64V256H128zM832 256v576h64V256h-64z";
        var pathString2 =
            "M230.08 738.56l58.816 58.88L574.336 512l-285.44-285.44-58.88 58.88L456.704 512l-226.56 226.56z m487.04 29.44V256h-83.2v512h83.2z";

        _ = pathFigureCollectionConverter.ConvertFrom(pathString1);
        _ = pathFigureCollectionConverter.ConvertFrom(pathString2);

        for (int i = 0; i < 100; i++)
        {
            Task.Run(() =>
            {
                var pathFigureCollection = pathFigureCollectionConverter.ConvertFrom(pathString1);
            });
        }

        for (int i = 0; i < 100; i++)
        {
            Task.Run(() =>
            {
                var pathFigureCollection = pathFigureCollectionConverter.ConvertFrom(pathString2);
            });
        }
    }

    private void OnCounterClicked(object sender, EventArgs e)
    {
        count++;

        if (count == 1)
            CounterBtn.Text = $"Clicked {count} time";
        else
            CounterBtn.Text = $"Clicked {count} times";

        SemanticScreenReader.Announce(CounterBtn.Text);
    }
}