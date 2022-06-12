using GraphicsTester.Scenarios;

using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Xaml;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RijoqicainejoHifolurqall;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        _canvas.Canvas = Canvas;

        foreach (var scenario in ScenarioList.Scenarios)
        {
            List.Items.Add(scenario);
        }
        List.SelectionChanged += (source, args) => Drawable = List.SelectedItem as IDrawable;

        List.SelectedIndex = 0;

        this.SizeChanged += (source, args) => Draw();
    }

	public IDrawable Drawable
	{
		get => _drawable;
		set
		{
			_drawable = value;
			Draw();
		}
	}

	private void Draw()
	{
		if (_drawable != null)
		{
			using (_canvas.CreateSession())
			{
				_drawable.Draw(_canvas, new RectF(0, 0, (float) Canvas.Width, (float) Canvas.Height));
			}
		}
	}

	private readonly XamlCanvas _canvas = new XamlCanvas();
    private IDrawable _drawable;
}
