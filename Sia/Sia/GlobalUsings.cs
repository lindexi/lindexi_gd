global using System.Collections.Immutable;

global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Localization;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;

global using Sia.Models;
global using Sia.Presentation;
global using Sia.DataContracts;
global using Sia.DataContracts.Serialization;
global using Sia.Services.Caching;
global using Sia.Services.Endpoints;

#if MAUI_EMBEDDING
global using Sia.MauiControls;
#endif
global using ApplicationExecutionState = Windows.ApplicationModel.Activation.ApplicationExecutionState;

global using CommunityToolkit.Mvvm.ComponentModel;
global using CommunityToolkit.Mvvm.Input;
