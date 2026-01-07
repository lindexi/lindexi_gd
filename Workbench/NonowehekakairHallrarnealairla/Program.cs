// See https://aka.ms/new-console-template for more information

ShowTypes showTypes = ShowTypes.Full;

Console.WriteLine(showTypes.ToString());

showTypes = ShowTypes.Graduation | ShowTypes.GraduationTag;
Console.WriteLine(showTypes.ToString());

[Flags]
public enum ShowTypes
{
    /// <summary>
    /// 全部显示
    /// </summary>
    Full = Graduation | GraduationTag | CoordinateAxes | Mesh,
  
    Graduation = 0x00000001,

    GraduationTag = 0x00000010,

    CoordinateAxes = 0x00000100,
   
    Mesh = 0x00001000,

    None = 0x00010000
}