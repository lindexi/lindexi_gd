// See https://aka.ms/new-console-template for more information

using DocumentFormat.OpenXml.Drawing;

SchemeColor color = new SchemeColor()
{
    Val = SchemeColorValues.Accent1
};
SchemeColorValues value = color.Val.Value;
Console.WriteLine(value);