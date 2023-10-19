// See https://aka.ms/new-console-template for more information


using System.Text.Json;
using System.Text.Json.Serialization;

var foo = new Foo()
{
    SaveType = CoursewareMergeExpandCompletionSaveType.SaveMergeCourseware
};

var json = JsonSerializer.Serialize(foo);

Console.WriteLine("Hello, World!");

public class Foo
{
    [JsonPropertyName("saveType")]
    public CoursewareMergeExpandCompletionSaveType SaveType { get; init; }
}

public enum CoursewareMergeExpandCompletionSaveType
{
    Fail = -1,
    SaveMergeCourseware = 0,
    SaveBeautifulCourseware = 1,
    TakeCoursewareImage = 2,
}