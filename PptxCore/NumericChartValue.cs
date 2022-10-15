namespace PptxCore;

/// <summary>
///     图表里面的值
/// </summary>
/// 可以是字符串类型，也可以是数值类型
/// 先放在一个类型里面，后续也许会拆分
/// <param name="NumericValueText"></param>
/// <param name="NumericPointFormatCodeText"></param>
/// <param name="UseDate1904">
///     This element specifies that the chart uses the 1904 date system. If the 1904 date system is used, then all dates
///     and times shall be specified as a decimal number of days since Dec. 31, 1903. If the 1904 date system is not
///     used, then all dates and times shall be specified as a decimal number of days since Dec. 31, 1899.
///     [Date systems in
///     Excel](https://support.microsoft.com/en-us/office/date-systems-in-excel-e7fe7167-48a9-4b96-bb53-5612a800b487 )
/// </param>
public record NumericChartValue
    (string? NumericValueText, string? NumericPointFormatCodeText, bool UseDate1904) : INumericChartValue, IChartValue
{
    public string? GetViewText()
    {
        if (NumericValueText != null)
        {
            if (IsTimeFormat())
            {
                // 解析方法请看
                // [dotnet OpenXML 解析 PPT 图表 解析日期时间表示内容](https://blog.lindexi.com/post/dotnet-OpenXML-%E8%A7%A3%E6%9E%90-PPT-%E5%9B%BE%E8%A1%A8-%E8%A7%A3%E6%9E%90%E6%97%A5%E6%9C%9F%E6%97%B6%E9%97%B4%E8%A1%A8%E7%A4%BA%E5%86%85%E5%AE%B9.html )
                var days = GetValue();
                days--; // 不包括当天

                var format = NumericPointFormatCodeText;
                if (format == null || format == "m/d/yyyy")
                {
                    // 如果是空和默认的，转换为中文的。后续可以根据设备的语言，转换为对应的日期
                    format = "yyyy/M/d";
                }

                if (UseDate1904)
                {
                    return new DateTime(1903, 12, 31).AddDays(days).ToString(format);
                }

                return new DateTime(1899, 12, 31).AddDays(days).ToString(format);
            }

            return NumericValueText;
        }

        return null;
    }

    /// <inheritdoc />
    public double GetValue()
    {
        if (NumericValueText is not null)
        {
            if (double.TryParse(NumericValueText, out var value))
            {
                return value;
            }
        }

        return 0;
    }

    /// <summary>
    ///     字符串格式化是否时间格式化字符串
    /// </summary>
    /// <returns></returns>
    private bool IsTimeFormat()
    {
        return NumericPointFormatCodeText?.Contains("yy") ?? false;
    }

    public override string ToString()
    {
        return
            $"ViewText={GetViewText()};NumericValueText={NumericValueText};NumericPointFormatCodeText={NumericPointFormatCodeText}";
    }
}