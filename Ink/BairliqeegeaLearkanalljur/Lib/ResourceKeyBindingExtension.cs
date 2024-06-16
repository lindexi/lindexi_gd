using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows;

namespace Lib;

public class ResourceKeyBindingExtension : MarkupExtension
{
    public sealed override object ProvideValue(IServiceProvider serviceProvider)
    {
        Binding targetElementBinding = new Binding
        {
            RelativeSource = new RelativeSource
            {
                Mode = RelativeSourceMode.Self
            }
        };

        Binding resourceKeyBinding = new Binding
        {
            BindsDirectlyToSource = BindsDirectlyToSource,
            Mode = BindingMode.OneWay,
            Path = Path,
            XPath = XPath,
        };

        if (ElementName != null)
        {
            resourceKeyBinding.ElementName = ElementName;
        }
        else if (RelativeSource != null)
        {
            resourceKeyBinding.RelativeSource = RelativeSource;
        }
        else if (Source != null)
        {
            resourceKeyBinding.Source = Source;
        }

        // 创建一个 Converter，指定如何从元素和属性转换为 Resource Object。
        DependencyProperty? targetProperty = serviceProvider.GetTargetProperty<DependencyProperty>();
        var resourceKeyConverter = new ResourceKeyToResourceConverter
        {
            ResourceKeyConverter = Converter,
            ConverterParameter = ConverterParameter,
            StringFormat = StringFormat,
            FindResource = (element, o) =>
            {
                if (element == null || o == null)
                {
                    return null;
                }
                return ProvideValue(element, targetProperty, o);
            },
        };

        var multiBinding = new MultiBinding
        {
            Converter = resourceKeyConverter,
        };
        multiBinding.Bindings.Add(targetElementBinding);
        multiBinding.Bindings.Add(resourceKeyBinding);

        return multiBinding.ProvideValue(serviceProvider);
    }

    protected virtual object ProvideValue(FrameworkElement targetElement,
         DependencyProperty? targetProperty, object resourceKey)
    {
        return targetElement.TryFindResource(resourceKey);
    }

    public ResourceKeyBindingExtension()
    {
    }

    public ResourceKeyBindingExtension(PropertyPath path)
    {
        Path = path;
    }

    [DefaultValue(false)]
    public bool BindsDirectlyToSource { get; set; }

    [DefaultValue(null)]
    public IValueConverter? Converter { get; set; }

    [DefaultValue(null)]
    public object? ConverterParameter { get; set; }

    [DefaultValue(null)]
    public string? ElementName { get; set; }

    [ConstructorArgument("path")]
    public PropertyPath? Path { get; set; }

    [DefaultValue(null)]
    public RelativeSource? RelativeSource { get; set; }

    public object? Source { get; set; }

    [DefaultValue(null)]
    public string? StringFormat { get; set; }

    [DefaultValue(null)]
    public string? XPath { get; set; }
}