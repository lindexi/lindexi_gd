using System.IO;
using System.Xml.Serialization;

namespace WpfApp.InkDataModels;

public class ArrayOfArrayOfInkDataModel : List<ArrayOfInkDataModel>
{
    public static ArrayOfArrayOfInkDataModel ReadFromFile(string filePath)
    {
        var xmlSerializer = new XmlSerializer(typeof(ArrayOfArrayOfInkDataModel));
        var file = Path.GetFullPath(filePath);
        var xml = File.ReadAllText(file);

        var result = (ArrayOfArrayOfInkDataModel) xmlSerializer.Deserialize(new StringReader(xml))!;
        return result;
    }
}