using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.UISpy.Uno.Tree;

[DebuggerDisplay("{TypeName,nq} ({Name,nq}) [{Children.Length,nq}]")]
public record ElementProxy(DependencyObject Element, ImmutableArray<ElementProxy> Children)
{
    public DependencyObject Element { get; init; } = Element;

    public string TypeName { get; } = Element.GetType().Name;

    public string FullTypeName { get; } = Element.GetType().FullName!;

    public string? Name { get; } = Element.GetValue(FrameworkElement.NameProperty) as string ?? Element.GetType().Name;

    public ImmutableArray<ElementPropertyProxy> GetProperties()
    {
        var properties = new List<ElementPropertyProxy>();
        var propertyDescriptors = TypeDescriptor.GetProperties(Element);

        foreach (PropertyDescriptor propertyDescriptor in propertyDescriptors)
        {
            try
            {
                var value = propertyDescriptor.GetValue(Element);
                properties.Add(new ElementPropertyProxy(
                    Element,
                    propertyDescriptor.Name,
                    value,
                    propertyDescriptor.PropertyType.Name));
            }
            catch (Exception exception)
            {
                object value = exception;
                var isNotImplemented = false;
                if (exception is TargetInvocationException targetInvocationException)
                {
                    if (targetInvocationException.InnerException is NotImplementedException)
                    {
                        isNotImplemented = true;
                        value = "NotImplemented";
                    }
                }

                properties.Add(new ElementPropertyProxy(
                    Element,
                    propertyDescriptor.Name,
                    value,
                    propertyDescriptor.PropertyType.Name)
                {
                    IsFailed = true,
                    IsNotImplemented = isNotImplemented
                });
            }
        }

        properties.Sort((a, b) => string.Compare(a.PropertyName, b.PropertyName, StringComparison.Ordinal));

        return [.. properties];
    }

    public static ElementProxy Create(DependencyObject element, List<ElementProxy> children)
    {
        return new ElementProxy(element, [.. children]);
    }
}
