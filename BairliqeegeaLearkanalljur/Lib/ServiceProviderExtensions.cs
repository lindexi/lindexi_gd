using System.Diagnostics.CodeAnalysis;
using System.Windows.Markup;

namespace Lib;

internal static class ServiceProviderExtensions
{
    internal static T? GetTargetProperty<T>([NotNull] this IServiceProvider serviceProvider) where T : class
    {
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
        IProvideValueTarget? service =
            serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
        return service == null ? null : service.TargetProperty as T;
    }
}