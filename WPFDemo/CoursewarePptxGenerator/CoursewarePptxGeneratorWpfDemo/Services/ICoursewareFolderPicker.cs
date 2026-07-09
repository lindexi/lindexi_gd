namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Selects a courseware folder for the open courseware workflow.
/// </summary>
public interface ICoursewareFolderPicker
{
    /// <summary>
    /// Shows the folder picker and returns the selected folder path.
    /// </summary>
    /// <returns>The selected folder path, or <see langword="null" /> when the user cancels.</returns>
    string? PickCoursewareFolder();
}
