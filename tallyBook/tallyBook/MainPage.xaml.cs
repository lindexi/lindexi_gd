using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

//“空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 上有介绍

namespace tallyBook
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        viewModel view;
        public MainPage()
        {
            view = new viewModel();
            this.InitializeComponent();
            xf.Navigate(typeof(book));

            redit.Document.SetText(TextSetOptions.None , @"老周：当RichEditBox控件的上下文菜单即将弹出时，会引发ContextMenuOpening事件，我们需要处理该事件，并且将e.Handled属性设置为true，这样才能阻止默认上下文菜单的弹出
通过FlyoutBase类的AttachedFlyout附加属性，可以将派出自FlyoutBase的弹出对象附加到任意的可视化对象上，由于这里咱们用的菜单，所以附加到RichEditBox控件的弹出对象为MenuFlyout实例。");           
        }

        private void Button_Click(object sender , RoutedEventArgs e)
        {
            tb.Focus(FocusState.Keyboard);

            
        }

        private void OnCopy(object sender , RoutedEventArgs e)
        {
            //复制
            redit.Document.Selection.Copy();
        }

        private void OnCut(object sender , RoutedEventArgs e)
        {
            //剪切
            redit.Document.Selection.Cut();
        }

        private void OnPaste(object sender , RoutedEventArgs e)
        {
            //粘贴
            redit.Document.Selection.Paste(0);
            //Paste 要在粘贴操作中使用的剪贴板格式。零表示最佳格式
        }

        /// <summary>
        /// 设置字体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFontSize(object sender , RoutedEventArgs e)
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            // 获取字号
            float size = Convert.ToSingle(item.Tag);

            redit.Document.Selection.CharacterFormat.Size = size;
        }
        /// <summary>
        /// 加粗
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBold(object sender , RoutedEventArgs e)
        {
            //using Windows.UI.Text;
            ToggleMenuFlyoutItem item = sender as ToggleMenuFlyoutItem;
            redit.Document.Selection.CharacterFormat.Bold = item.IsChecked ? FormatEffect.On : FormatEffect.Off;
        }

        private void OnUnderline(object sender , RoutedEventArgs e)
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            int x = Convert.ToInt32(item.Tag);
            UnderlineType unlinetp;
            switch (x)
            {
                case -1: // 无
                    unlinetp = UnderlineType.None;
                    break;
                case 0: // 单实线
                    unlinetp = UnderlineType.Single;
                    break;
                case 1: // 双实线
                    unlinetp = UnderlineType.Double;
                    break;
                case 2: // 虚线
                    unlinetp = UnderlineType.Dash;
                    break;
                default:
                    unlinetp = UnderlineType.None;
                    break;
            }
            redit.Document.Selection.CharacterFormat.Underline = unlinetp;
        }

        private void OnContextMenuOpening(object sender , ContextMenuEventArgs e)
        {
            //阻止弹出默认的上下文菜单，然后，调用ShowAt方法在指定的坐标处打开菜单
            e.Handled = true;
            MenuFlyout menu = FlyoutBase.GetAttachedFlyout(redit) as MenuFlyout;
            menu?.ShowAt(redit , new Point(e.CursorLeft , e.CursorTop));
        }

        private void OnTinct(object sender , RoutedEventArgs e)
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            string tinct = item.Tag as string;
            Windows.UI.Color color = new Windows.UI.Color();
            switch (tinct)
            {
                case "黑色":
                    color= Windows.UI.Colors.Black;
                    break;
                case "蓝色":
                    color = Windows.UI.Colors.Blue;
                    break;
                case "白色":
                    color = Windows.UI.Colors.White;
                    break;
                default:
                    break;
            }
            redit.Document.Selection.CharacterFormat.BackgroundColor = color;
        }
    }
}
