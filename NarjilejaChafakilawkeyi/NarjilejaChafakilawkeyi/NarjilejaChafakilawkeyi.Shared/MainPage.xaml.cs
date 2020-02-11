using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Color = Windows.UI.Color;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace NarjilejaChafakilawkeyi
{
    public enum CurrentConvert
    {
        Main,
        ColorConvert,
        DecimalConvert,
        Base64Convert,
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            Loaded += MainPage_Loaded;

            GotoSuggestBox.ItemsSource = new ObservableCollection<string>()
            {
                "颜色转换", "进制转换","base64转换","随机命名"
            };

            GotoSuggestBox.TextChanged += GotoSuggestBox_TextChanged;
            GotoSuggestBox.SuggestionChosen += GotoSuggestBox_SuggestionChosen;
        }

        private void GotoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            var selectedItem = args.SelectedItem;
        }

        private void GotoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                
            }
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            GotoSuggestBox.Focus(FocusState.Programmatic);
        }

        private void ColorConvert_OnChecked(object sender, RoutedEventArgs e)
        {
            SetAllCollapsed();

            ColorConvertGrid.Visibility = Visibility.Visible;
        }

        private void DecimalConvert_OnChecked(object sender, RoutedEventArgs e)
        {
            SetAllCollapsed();

            DecimalConvertGrid.Visibility = Visibility.Visible;
        }

        private void Base64Convert_OnChecked(object sender, RoutedEventArgs e)
        {
            SetAllCollapsed();

            Base64ConvertGrid.Visibility = Visibility.Visible;
        }

        private void Whitman_OnChecked(object sender, RoutedEventArgs e)
        {
            SetAllCollapsed();

            WhitmanGrid.Visibility = Visibility.Visible;
        }

        private void SetAllCollapsed()
        {
            Base64ConvertGrid.Visibility = Visibility.Collapsed;
            ColorConvertGrid.Visibility = Visibility.Collapsed;
            DecimalConvertGrid.Visibility = Visibility.Collapsed;
            WhitmanGrid.Visibility = Visibility.Collapsed;

            GotoSuggestBox.Visibility = Visibility.Collapsed;
        }

        private void ColorText_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                ColorBorder.Background = GetSolidColorBrush(ColorText.Text);
            }
            catch (Exception)
            {

            }
        }

        public SolidColorBrush GetSolidColorBrush(string hex)
        {
            hex = hex.Replace("#", string.Empty);

            //#FFDFD991
            //#DFD991
            //#FD92
            //#DAC

            bool existAlpha = hex.Length == 8 || hex.Length == 4;
            bool isDoubleHex = hex.Length == 8 || hex.Length == 6;

            if (!existAlpha && hex.Length != 6 && hex.Length != 3)
            {
                throw new ArgumentException("输入的hex不是有效颜色");
            }

            int n = 0;
            byte a;
            int hexCount = isDoubleHex ? 2 : 1;
            if (existAlpha)
            {
                n = hexCount;
                a = (byte)ConvertHexToByte(hex, 0, hexCount);
                if (!isDoubleHex)
                {
                    a = (byte)(a * 16 + a);
                }
            }
            else
            {
                a = 0xFF;
            }

            var r = (byte)ConvertHexToByte(hex, n, hexCount);
            var g = (byte)ConvertHexToByte(hex, n + hexCount, hexCount);
            var b = (byte)ConvertHexToByte(hex, n + 2 * hexCount, hexCount);
            if (!isDoubleHex)
            {
                //#FD92 = #FFDD9922

                r = (byte)(r * 16 + r);
                g = (byte)(g * 16 + g);
                b = (byte)(b * 16 + b);
            }

            return new SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
        }

        private static uint ConvertHexToByte(string hex, int n, int count = 2)
        {
            return Convert.ToUInt32(hex.Substring(n, count), 16);
        }


        private void DecimalText_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var text = DecimalText.Text;


                if (int.TryParse(text, out var n))
                {
                    HexText.Text = n.ToString("X");
                    OctalText.Text = ToOctal(n);
                }
            }
            catch (Exception)
            {

            }
        }

        private static string ToOctal(int n)
        {
            int i = 0;
            int a, b = 0;
            while (n > 0)
            {
                a = n % 8;
                n = n / 8;
                b = b + a * (int)Math.Pow(10, i);
                i++;
            }

            return b.ToString();
        }

        private void HexText_OnTextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void OctalText_OnTextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void ToolItem_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetAllCollapsed();

            if (e.AddedItems.Count == 0)
            {
                var select = e.AddedItems[0];


                if (select == ColorConvertText)
                {
                    ColorConvertGrid.Visibility = Visibility.Visible;
                }
                else if (select == DecimalConvertText)
                {
                    DecimalConvertGrid.Visibility = Visibility.Visible;
                }
                else if (select == Base64ConvertText)
                {
                    Base64ConvertGrid.Visibility = Visibility.Visible;
                }
                else if (select == WhitmanConvertText)
                {
                    WhitmanGrid.Visibility = Visibility.Visible;
                }

            }
        }
    }
}
