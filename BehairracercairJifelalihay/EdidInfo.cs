namespace BehairracercairJifelalihay;

public readonly record struct EdidInfo
{
    public string ManufacturerName => new string([ManufacturerNameChar0, ManufacturerNameChar1, ManufacturerNameChar2]);

    public char ManufacturerNameChar0 { get; init; }
    public char ManufacturerNameChar1 { get; init; }
    public char ManufacturerNameChar2 { get; init; }

    public byte ManufactureWeek { get; init; }
    
    /// <summary>
    /// 已加上 1990 的年份
    /// </summary>
    public int ManufactureYear { get; init; }

    public byte Version { get; init; }
    public byte Revision { get; init; }

    public Version EdidVersion => new Version(Version, Revision);

    /// <summary>
    /// See Section 3.6
    /// </summary>
    public EdidBasicDisplayParameters BasicDisplayParameters { get; init; }
}