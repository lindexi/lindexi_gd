using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DeepSeekWpf.Models;
using DeepSeekWpf.ViewModels;

namespace DeepSeekWpf.Views;

public partial class ChatView : UserControl
{
    private readonly DispatcherTimer _scrollTimer;
    private ChatWorkspaceViewModel? _viewModel;
    private ChatSession? _observedSession;
    private bool _isNearBottom = true;
    private bool _forceScroll;

    public ChatView()
    {
        InitializeComponent();
        _scrollTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(75), DispatcherPriority.Background, OnScrollTimerTick, Dispatcher)
        {
            IsEnabled = false,
        };
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public void FocusSessionSearch()
    {
        SessionSearchBox.Focus();
        SessionSearchBox.SelectAll();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null && DataContext is ChatWorkspaceViewModel viewModel)
        {
            _viewModel = viewModel;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            AttachSession(_viewModel.SelectedSession);
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        DetachViewModel();
        _viewModel = e.NewValue as ChatWorkspaceViewModel;
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        AttachSession(_viewModel.SelectedSession);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChatWorkspaceViewModel.SelectedSession))
        {
            AttachSession(_viewModel?.SelectedSession);
            _forceScroll = true;
            ScheduleScroll();
        }
    }

    private void AttachSession(ChatSession? session)
    {
        if (_observedSession is not null)
        {
            _observedSession.Messages.CollectionChanged -= OnMessagesChanged;
            foreach (var message in _observedSession.Messages)
            {
                message.PropertyChanged -= OnMessagePropertyChanged;
            }
        }

        _observedSession = session;
        if (_observedSession is null)
        {
            return;
        }

        _observedSession.Messages.CollectionChanged += OnMessagesChanged;
        foreach (var message in _observedSession.Messages)
        {
            message.PropertyChanged += OnMessagePropertyChanged;
        }
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (ChatMessageViewModel message in e.OldItems)
            {
                message.PropertyChanged -= OnMessagePropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (ChatMessageViewModel message in e.NewItems)
            {
                message.PropertyChanged += OnMessagePropertyChanged;
            }
        }

        ScheduleScroll();
    }

    private void OnMessagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ChatMessageViewModel.Content) or nameof(ChatMessageViewModel.ThoughtContent))
        {
            ScheduleScroll();
        }
    }

    private void OnMessagesScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        _isNearBottom = e.ExtentHeight - e.ViewportHeight - e.VerticalOffset <= 80;
    }

    private void ScheduleScroll()
    {
        if (!_isNearBottom && !_forceScroll)
        {
            return;
        }

        _scrollTimer.Stop();
        _scrollTimer.Start();
    }

    private void OnScrollTimerTick(object? sender, EventArgs e)
    {
        _scrollTimer.Stop();
        if (MessagesList.Items.Count > 0 && (_isNearBottom || _forceScroll))
        {
            MessagesList.ScrollIntoView(MessagesList.Items[^1]);
        }

        _forceScroll = false;
    }

    private void OnMessageInputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_viewModel is null ||
            e.Key != Key.Enter ||
            !Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            return;
        }

        if (!_viewModel.SendMessageCommand.CanExecute(null))
        {
            return;
        }

        e.Handled = true;
        _forceScroll = true;
        _viewModel.SendMessageCommand.Execute(null);
        var inputBox = (TextBox)sender;
        Dispatcher.BeginInvoke(() => inputBox.Focus(), DispatcherPriority.Input);
    }

    private void OnSendButtonClick(object sender, RoutedEventArgs e)
    {
        _forceScroll = true;
        ScheduleScroll();
        Dispatcher.BeginInvoke(() => MessageInputBox.Focus(), DispatcherPriority.Input);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _scrollTimer.Stop();
        DetachViewModel();
    }

    private void DetachViewModel()
    {
        AttachSession(null);
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel = null;
        }
    }
}
