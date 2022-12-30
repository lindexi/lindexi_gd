namespace BefawafereKehufallkee;

public interface IClrValueConverter
{
    object Convert(object value, Type targetType, object parameter, out bool success);
    object ConvertBack(object value, Type targetType, object parameter, out bool success);
}