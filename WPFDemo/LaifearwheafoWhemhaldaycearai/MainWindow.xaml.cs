using AdaptiveCards.Rendering.Wpf;

using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AdaptiveCards;
using AdaptiveCards.Rendering;

namespace LaifearwheafoWhemhaldaycearai;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var adaptiveCardRenderer = new AdaptiveCardRenderer(new AdaptiveHostConfig()
        {
            
        });
        var adaptiveCard = new AdaptiveCard(new AdaptiveSchemaVersion(1,0))
        {
            InternalID = new AdaptiveInternalID()
        };
        adaptiveCard.Body.Add(new AdaptiveTextBlock()
        {
            Text = "Hello",
            Size = AdaptiveTextSize.ExtraLarge,
        });
        var json = adaptiveCard.ToJson();

        var result = adaptiveCardRenderer.RenderCard(adaptiveCard);
        RootGrid.Children.Add(result.FrameworkElement);
    }
}