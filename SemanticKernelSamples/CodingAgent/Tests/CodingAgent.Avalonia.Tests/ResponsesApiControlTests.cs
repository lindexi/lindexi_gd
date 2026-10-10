using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using CodingChatRoom.AvaloniaShell.ViewModels;
using CodingChatRoom.AvaloniaShell.Views;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class ResponsesApiControlTests
{
    [TestMethod]
    [DataRow(false, 0d)]
    [DataRow(true, 1d)]
    public void ResponsesApiShouldShowCheckOnlyWhenSelected(bool selected, double expectedOpacity)
    {
        using var viewModel = new ChatViewModel { IsResponsesApiEnabled = selected };
        var view = new ChatView { DataContext = viewModel };
        var window = new Window { Width = 1016, Height = 620, Content = view };
        try
        {
            window.Show();
            window.UpdateLayout();
            var toggle = view.FindControl<ToggleButton>("ResponsesApiToggleButton")
                ?? throw new InvalidOperationException("Responses toggle missing.");
            var check = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(toggle)
                .OfType<Avalonia.Controls.Shapes.Path>().Single(item => item.Name == "ApiModeCheck");

            Assert.AreEqual(expectedOpacity, check.Opacity);
        }
        finally { window.Close(); }
    }

    [TestMethod]
    [DataRow(false, ":pointerover")]
    [DataRow(true, ":pointerover")]
    [DataRow(false, ":pressed")]
    [DataRow(true, ":pressed")]
    public void ResponsesApiTextBackgroundShouldRemainTransparent(bool selected, string state)
    {
        var toggle = new StateToggleButton
        {
            Content = "Responses API",
            IsChecked = selected,
            Classes = { "ApiMode" },
        };
        var window = new Window { Content = toggle };
        try
        {
            window.Show();
            toggle.SetState(state);
            var presenter = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(toggle)
                .OfType<Avalonia.Controls.Presenters.ContentPresenter>()
                .Single(item => item.Name == "ApiModeContent");

            Assert.AreEqual(Avalonia.Media.Colors.Transparent,
                ((Avalonia.Media.ISolidColorBrush)presenter.Background!).Color);
        }
        finally { window.Close(); }
    }

    private sealed class StateToggleButton : ToggleButton
    {
        protected override Type StyleKeyOverride => typeof(ToggleButton);

        public void SetState(string state) => PseudoClasses.Set(state, true);
    }

    [TestMethod]
    public void ResponsesApiShouldBeDisabledByDefault()
    {
        using var viewModel = new ChatViewModel();

        Assert.IsFalse(viewModel.IsResponsesApiEnabled);
    }

    [TestMethod]
    public void ChangingResponsesApiShouldNotifyOnce()
    {
        using var viewModel = new ChatViewModel();
        var changedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        viewModel.IsResponsesApiEnabled = true;
        viewModel.IsResponsesApiEnabled = true;

        CollectionAssert.AreEqual(new[] { nameof(ChatViewModel.IsResponsesApiEnabled) }, changedProperties);
    }

    [TestMethod]
    public void SelectingResponsesApiShouldUpdateViewModel()
    {
        using var viewModel = new ChatViewModel();
        var view = new ChatView { DataContext = viewModel };
        var toggle = view.FindControl<ToggleButton>("ResponsesApiToggleButton")
            ?? throw new InvalidOperationException("Responses toggle missing.");

        toggle.IsChecked = true;

        Assert.IsTrue(viewModel.IsResponsesApiEnabled);
    }

    [TestMethod]
    public void ChangingViewModelShouldUpdateResponsesApiToggle()
    {
        using var viewModel = new ChatViewModel();
        var view = new ChatView { DataContext = viewModel };
        var toggle = view.FindControl<ToggleButton>("ResponsesApiToggleButton")
            ?? throw new InvalidOperationException("Responses toggle missing.");

        viewModel.IsResponsesApiEnabled = true;

        Assert.AreEqual(true, toggle.IsChecked);
    }

    [TestMethod]
    public void ResponsesApiShouldHaveMatchingAccessibleName()
    {
        using var viewModel = new ChatViewModel();
        var view = new ChatView { DataContext = viewModel };
        var toggle = view.FindControl<ToggleButton>("ResponsesApiToggleButton")
            ?? throw new InvalidOperationException("Responses toggle missing.");

        Assert.AreEqual(toggle.Content, AutomationProperties.GetName(toggle));
    }

    [TestMethod]
    public void ResponsesApiShouldAcceptKeyboardNavigationFocus()
    {
        using var viewModel = new ChatViewModel();
        var view = new ChatView { DataContext = viewModel };
        var window = new Window { Width = 1016, Height = 620, Content = view };
        try
        {
            window.Show();
            var toggle = view.FindControl<ToggleButton>("ResponsesApiToggleButton")
                ?? throw new InvalidOperationException("Responses toggle missing.");

            toggle.Focus(NavigationMethod.Tab);

            Assert.IsTrue(toggle.IsFocused);
        }
        finally { window.Close(); }
    }

    [TestMethod]
    [DataRow(640)]
    [DataRow(760)]
    [DataRow(1016)]
    public void RunOptionsShouldStayInsideTheirColumn(int width)
    {
        using var viewModel = new ChatViewModel();
        var view = new ChatView { DataContext = viewModel };
        var window = new Window { Width = width, Height = 620, Content = view };
        try
        {
            window.Show();
            window.UpdateLayout();
            var options = view.FindControl<WrapPanel>("RunOptionsPanel")
                ?? throw new InvalidOperationException("Run options missing.");

            Assert.IsTrue(options.Children.All(child => new Rect(options.Bounds.Size).Contains(child.Bounds.BottomRight)));
        }
        finally { window.Close(); }
    }
}
