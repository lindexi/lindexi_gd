using Microsoft.Maui.Graphics;

namespace WejallkachawDadeawejearhuce.Wpf.Inking;

static class MauiPointExtension
{
    public static System.Windows.Point ToWpfPoint(this Point point)
    {
        return new System.Windows.Point(point.X, point.Y);
    }
}