using CoursewarePptxGeneratorWpfDemo.Services;

namespace CoursewarePptxGeneratorWpfDemo.Tests.Fakes;

internal sealed class FakeCoursewareFolderPicker(string? folderPath) : ICoursewareFolderPicker
{
    public string? PickCoursewareFolder()
    {
        return folderPath;
    }
}
