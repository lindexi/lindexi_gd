using System.Text;

using Microsoft.UI.Input;

namespace JerelojecairhearGecurbobe;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        PointerPressed += MainPage_PointerPressed;
        PointerMoved += MainPage_PointerMoved;
        PointerReleased += MainPage_PointerReleased;
    }

    private void MainPage_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        PointerDictionary[e.Pointer.PointerId] = new PointerInfo(e.Pointer.PointerId, e.GetCurrentPoint(this));
        Update();
    }

    private void MainPage_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (PointerDictionary.ContainsKey(e.Pointer.PointerId))
        {
            PointerDictionary[e.Pointer.PointerId] = new PointerInfo(e.Pointer.PointerId, e.GetCurrentPoint(this));
            Update();
        }
    }

    private void MainPage_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        PointerDictionary.Remove(e.Pointer.PointerId);
        Update();
    }

    public void Update()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"Count={PointerDictionary.Count}");
        foreach (var pointerInfo in PointerDictionary.Values)
        {
            var pointerPoint = pointerInfo.PointerPoint;

            stringBuilder.AppendLine($"[{pointerInfo.PointerId}] Id={pointerPoint.PointerId};FrameId={pointerPoint.FrameId};Position={pointerPoint.Position};Rect={pointerPoint.Properties.ContactRect}");
        }

        TextBlock.Text = stringBuilder.ToString();
    }

    public Dictionary<uint, PointerInfo> PointerDictionary { get; } = new Dictionary<uint, PointerInfo>();

    public record PointerInfo(uint PointerId, PointerPoint PointerPoint)
    {
    }
}
