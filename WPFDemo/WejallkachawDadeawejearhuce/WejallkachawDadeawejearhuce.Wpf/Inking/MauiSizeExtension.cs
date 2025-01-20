using Microsoft.Maui.Graphics;

namespace WejallkachawDadeawejearhuce.Wpf.Inking;

static class MauiSizeExtension
{
    public static System.Windows.Size ToWpfSize(this Size size)
    {
        return new System.Windows.Size(size.Width, size.Height);
    }
}