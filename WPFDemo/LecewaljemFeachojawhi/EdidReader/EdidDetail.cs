using System.Text;

namespace LecewaljemFeachojawhi.EdidReader;

/// <summary>
/// Presents parsed EDID information.
/// </summary>
public class EdidDetail
{
    public static EdidDetail Parse(byte[] data)
    {
        var detail = new EdidDetail();

        var hex = Encoding.ASCII.GetString(data);
        detail.Manufacturer = hex.Substring(90, 17).Trim().Replace("\0", string.Empty).Replace("?", string.Empty);
        detail.Model = hex.Substring(108, 17).Trim().Replace("\0", string.Empty).Replace("?", string.Empty);

        var DTD_START = 54;
        var HORIZONTAL_DISPLAY_TOP_OFFSET = 4;
        var HORIZONTAL_DISPLAY_TOP_MASK = 0x0F;
        var VERTICAL_DISPLAY_TOP_MASK = 0x0F;
        detail.HorizontalDisplaySize = (data[DTD_START + 14] >> HORIZONTAL_DISPLAY_TOP_OFFSET & HORIZONTAL_DISPLAY_TOP_MASK) << 8 | data[DTD_START + 12];
        detail.VerticalDisplaySize = (data[DTD_START + 14] & VERTICAL_DISPLAY_TOP_MASK) << 8 | data[DTD_START + 13];

        detail.HorizontalImageSize = data[21];
        detail.VerticalImageSize = data[22];

        return detail;
    }

    public string Manufacturer { get; private set; }

    public string Model { get; private set; }

    public int HorizontalDisplaySize { get; private set; }

    public int HorizontalImageSize { get; private set; }

    public int VerticalDisplaySize { get; private set; }

    public int VerticalImageSize { get; private set; }
}