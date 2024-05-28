using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.UI.Xaml.Data;

namespace UnoFileDownloader.Presentation.Converters
{
    public class VisibilityConverter : IValueConverter
    {
        public Visibility DefaultVisibility { set; get; }

        public bool? Visible { set; get; }

        /// <summary>
        /// 获取或设置 <see cref="Visibility.Collapsed"/> 所对应的值。
        /// </summary>
        public bool? Collapsed { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (Equals(value, Visible))
            {
                return Visibility.Visible;
            }
            if (Equals(value, Collapsed))
            {
                return Visibility.Collapsed;
            }
            return DefaultVisibility;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                throw new ArgumentNullException();
            }
            Visibility visibility = (Visibility) value;
            switch (visibility)
            {
                case Visibility.Visible:
                    return Visible;
                case Visibility.Collapsed:
                    return Collapsed;
                default:
                    throw new ArgumentException($"不支持指定值 {value} 的转换。");
            }
        }
    }
}
