using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System;

internal static partial class SR
{
    public static string? Size_CannotModifyEmptySize;
    public static string? Rect_CannotCallMethod;
    public static string? Size_HeightCannotBeNegative;
    public static string? Size_WidthCannotBeNegative;
    public static string? Rect_CannotModifyEmptyRect;
    public static string? Color_NullColorContext;
    public static string? Color_DimensionMismatch;
    public static string? Stylus_InvalidMax;
    public static Exception? InvalidStylusPointXYNaN;
    public static string? Size_WidthAndHeightCannotBeNegative;
    public static string? InvalidStylusPointDescription { get; set; }
    public static string? InvalidStylusPointDescriptionDuplicatesFound { get; set; }
    public static Exception? InvalidPressureValue { get; set; }
    public static string? InvalidAdditionalDataForStylusPoint { get; set; }
    public static string? InvalidStylusPointProperty { get; set; }
    public static Exception? InvalidMinMaxForButton { get; set; }
    public static string? InvalidStylusPointConstructionZeroLengthCollection { get; set; }
    public static string? IncompatibleStylusPointDescriptions { get; set; }
    public static string? InvalidStylusPointCollectionZeroCount { get; set; }
    public static string? InvalidStylusPointDescriptionButtonsMustBeLast { get; set; }
    public static string? InvalidStylusPointDescriptionTooManyButtons { get; set; }
    public static string? InvalidIsButtonForId { get; set; }
    public static string? InvalidIsButtonForId2 { get; set; }
    public static string? InvalidStylusPointPropertyInfoResolution { get; set; }

    public static string Get(string invalidGuid, params object[] p)
    {
        return string.Empty;
    }

    public static string? Format(string? a, params object[] p)
    {
        return a;
    }
}
