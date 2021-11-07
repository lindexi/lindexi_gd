using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Navigation;
using MS.Internal;


namespace dotnetCampus.WPF
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // 开始之前
            // 从 [CSDN 下载](https://download.csdn.net/download/lindexi_gd/13769715) 基础框架逻辑
            // 获取发一封邮件给我，让我发链接给你

            // 解压缩内容，放在 Lib 文件夹下，参考 dotnetCampus.WPF.csproj 的代码，确保文件都能找到

            var application = new Application();
            var window = new Window()
            {
                Title = "林德熙是逗比",
                Content = new Foo(),
            };
            window.Loaded += (sender, eventArgs) =>
            {
                // 这里的 GetAppWindow 是 internal 的方法，但是在这个程序集可以访问
                var navigationWindow = application.GetAppWindow();
            };
            application.Run(window);
        }
    }

    class Foo : UIElement
    {
        public Foo()
        {
            DrawingVisual drawingVisual = Visual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawRectangle(Brushes.CadetBlue, null, new Rect(10, 10, 100, 100));
            }
        }

        protected override int VisualChildrenCount => 1;
        protected override Visual GetVisualChild(int index)
        {
            return Visual;
        }

        private DrawingVisual Visual { get; }
    }
}
