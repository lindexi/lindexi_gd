using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using CodingAgent.AvaloniaShell.Services.Integrations.OpenAI;

namespace CodingAgent.AvaloniaShell.ViewModels;

public sealed class ModelCatalogViewModel : ViewModelBase, IDisposable
{
    private readonly ProviderSettingsViewModel _provider;
    private readonly IOpenAIModelCatalogClient _client;
    private CancellationTokenSource? _request;
    private string? _status;
    private string _modelId = string.Empty;

    internal ModelCatalogViewModel(ProviderSettingsViewModel provider, IOpenAIModelCatalogClient? client = null)
    {
        _provider = provider;
        _client = client ?? new OpenAIModelCatalogClient();
        LoadCommand = new SimpleAsyncCommand(LoadAsync, exceptionHandler: _ => Status = Text("SetupCatalogFailed"));
        AddCommand = new SimpleCommand<string>(Add);
        AddManualCommand = new SimpleCommand(() => { Cancel(); Add(ModelId); });
        _provider.PropertyChanged += OnConnectionChanged;
    }

    public ObservableCollection<string> Models { get; } = [];
    public SimpleAsyncCommand LoadCommand { get; }
    public SimpleCommand<string> AddCommand { get; }
    public SimpleCommand AddManualCommand { get; }
    public string ModelId { get => _modelId; set => SetField(ref _modelId, value); }
    public string? Status { get => _status; private set => SetField(ref _status, value); }

    internal static string Text(string key) => Application.Current?.FindResource(key) as string ?? key;

    internal static bool HasValidEndpoint(ProviderSettingsViewModel provider)
        => !string.IsNullOrWhiteSpace(provider.EndPoint)
           && Uri.TryCreate(provider.EndPoint.Trim(), UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

    internal static string EndpointValidationMessage(ProviderSettingsViewModel provider)
        => Text(string.IsNullOrWhiteSpace(provider.EndPoint) ? "SetupEndpointRequired" : "SetupInvalidEndpoint");

    internal async Task LoadAsync()
    {
        Cancel();
        Models.Clear();
        if (!HasValidEndpoint(_provider))
        {
            Status = EndpointValidationMessage(_provider);
            return;
        }

        using var request = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        _request = request;
        IsBusy = true;
        Status = Text("SetupCatalogLoading");
        try
        {
            var result = await _client.GetModelIdsAsync(_provider.EndPoint.Trim(), _provider.ApiKey, request.Token);
            if (_request != request || request.IsCancellationRequested) return;
            foreach (string id in result.ModelIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal))
                Models.Add(id);
            Status = result.IsSuccessful
                ? Text(Models.Count == 0 ? "SetupCatalogEmpty" : "SetupCatalogReady")
                : Text("SetupCatalogFailed");
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested)
        {
            if (_request == request) Status = Text("SetupCatalogTimeout");
        }
        finally
        {
            if (_request == request)
            {
                _request = null;
                IsBusy = false;
            }
        }
    }

    private void Add(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Status = Text("SetupModelRequired");
            return;
        }
        id = id.Trim();
        if (_provider.Models.Any(model => model.ModelId == id || model.ModelName == id))
        {
            Status = Text("SetupAlreadyAdded");
            return;
        }
        var model = _provider.Models.FirstOrDefault(model => string.IsNullOrWhiteSpace(model.Provider)
            && string.IsNullOrWhiteSpace(model.ModelName) && string.IsNullOrWhiteSpace(model.ModelId));
        if (model is null)
        {
            model = new ModelSettingsViewModel { Provider = "openai", ModelName = id, ModelId = id };
            _provider.Models.Add(model);
        }
        else
        {
            model.Provider = "openai";
            model.ModelName = id;
            model.ModelId = id;
        }
        ModelId = string.Empty;
        Status = Text("SetupModelAdded");
    }

    private void OnConnectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ProviderSettingsViewModel.EndPoint) or nameof(ProviderSettingsViewModel.ApiKey))
        {
            Cancel();
            Models.Clear();
            Status = null;
        }
    }

    internal void Cancel()
    {
        _request?.Cancel();
        _request = null;
        IsBusy = false;
    }

    public void Dispose()
    {
        Cancel();
        _provider.PropertyChanged -= OnConnectionChanged;
    }
}
