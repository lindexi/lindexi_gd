using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace AppLauncherWpf;

public partial class MainWindow : Window
{
    private const int LauncherHotKeyIdentifier = 1;

    private readonly Task<IReadOnlyList<ApplicationEntry>> applicationLoadTask;
    private readonly List<ApplicationEntry> applications = [];
    private readonly GlobalHotKey globalHotKey;
    private bool applicationsLoaded;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        applicationLoadTask = StartMenuApplicationCatalog.GetApplicationsAsync(CancellationToken.None);

        WindowInteropHelper windowInteropHelper = new(this);
        nint windowHandle = windowInteropHelper.EnsureHandle();
        globalHotKey = new GlobalHotKey(
            windowHandle,
            LauncherHotKeyIdentifier,
            Key.Space,
            OnLauncherHotKeyPressed);

        Closed += OnClosed;
    }

    internal ObservableCollection<ApplicationEntry> FilteredApplications { get; } = [];

    private async void OnLauncherHotKeyPressed()
    {
        if (IsVisible)
        {
            HideLauncher();
            return;
        }

        await ShowLauncherAsync();
    }

    private async Task ShowLauncherAsync()
    {
        Show();
        Activate();
        SearchTextBox.Focus();
        Keyboard.Focus(SearchTextBox);

        if (!applicationsLoaded)
        {
            try
            {
                applications.AddRange(await applicationLoadTask);
                applicationsLoaded = true;
            }
            catch (UnauthorizedAccessException)
            {
                ShowLoadError();
            }
            catch (IOException)
            {
                ShowLoadError();
            }
        }

        ApplyFilter();
    }

    private void OnSearchTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (applicationsLoaded)
        {
            ApplyFilter();
        }
    }

    private void ApplyFilter()
    {
        IEnumerable<ApplicationEntry> matches = ApplicationMatcher.Find(
            applications,
            SearchTextBox.Text);

        FilteredApplications.Clear();
        foreach (ApplicationEntry application in matches)
        {
            FilteredApplications.Add(application);
        }

        ApplicationsListBox.SelectedIndex = FilteredApplications.Count > 0 ? 0 : -1;
        EmptyStateTextBlock.Visibility = FilteredApplications.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                HideLauncher();
                e.Handled = true;
                break;
            case Key.Down:
                MoveSelection(1);
                e.Handled = true;
                break;
            case Key.Up:
                MoveSelection(-1);
                e.Handled = true;
                break;
            case Key.Enter:
                LaunchSelectedApplication();
                e.Handled = true;
                break;
        }
    }

    private void MoveSelection(int offset)
    {
        if (FilteredApplications.Count == 0)
        {
            return;
        }

        int nextIndex = Math.Clamp(
            ApplicationsListBox.SelectedIndex + offset,
            0,
            FilteredApplications.Count - 1);
        ApplicationsListBox.SelectedIndex = nextIndex;
        ApplicationsListBox.ScrollIntoView(ApplicationsListBox.SelectedItem);
    }

    private void OnApplicationDoubleClick(object sender, MouseButtonEventArgs e)
    {
        LaunchSelectedApplication();
    }

    private void LaunchSelectedApplication()
    {
        if (ApplicationsListBox.SelectedItem is not ApplicationEntry application)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(application.EntryPath)
            {
                UseShellExecute = true
            });
            HideLauncher();
        }
        catch (Win32Exception)
        {
            ShowLaunchError();
        }
        catch (InvalidOperationException)
        {
            ShowLaunchError();
        }
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        if (IsVisible)
        {
            HideLauncher();
        }
    }

    private void HideLauncher()
    {
        SearchTextBox.Clear();
        Hide();
    }

    private void ShowLoadError()
    {
        MessageBox.Show(
            this,
            (string)FindResource("ApplicationLoadFailedText"),
            (string)FindResource("ErrorCaption"),
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void ShowLaunchError()
    {
        MessageBox.Show(
            this,
            (string)FindResource("ApplicationLaunchFailedText"),
            (string)FindResource("ErrorCaption"),
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        globalHotKey.Dispose();
    }
}
