using System.Windows.Media;
using UnoInk.Inking.InkCore;

namespace WejallkachawDadeawejearhuce.Wpf.Inking;

class DynamicStrokeContext
{
    public FixedQueue<StylusPoint> StylusPointQueue { get; } = new FixedQueue<StylusPoint>(1000);
    public StreamGeometry StreamGeometry { get; } = new StreamGeometry();
}