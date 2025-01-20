using Microsoft.Maui.Graphics;

namespace WejallkachawDadeawejearhuce.Wpf.Inking;

static class MauiRectExtension
{
    public static System.Windows.Rect ToWpfRect(this Rect rect)
    {
        return new System.Windows.Rect(rect.X, rect.Y, rect.Width, rect.Height);
    }
}