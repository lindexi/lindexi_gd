using System.Windows;

namespace BefawafereKehufallkee;

public class ClrBindingPropertyContext
{
    public ClrBindingPropertyContext(object bindableObject, string path,
        PropertyGetter? propertyGetter = null,
        PropertySetter? propertySetter = null)
    {
        BindableObjectWeakReference = new WeakReference<object>(bindableObject);
        Path = path;
        PropertyGetter = propertyGetter;
        PropertySetter = propertySetter;
    }

    public WeakReference<object> BindableObjectWeakReference { get; }
    public string Path { get; }
    public PropertyGetter? PropertyGetter { get; }
    public PropertySetter? PropertySetter { get; }

    internal PropertyGetter? InternalPropertyGetter
    {
        get
        {
            if (_internalPropertyGetter is not null)
            {
                return _internalPropertyGetter;
            }

            _internalPropertyGetter ??= PropertyGetter;

            if (_internalPropertyGetter is null && BindableObjectWeakReference.TryGetTarget(out var bindableObject))
            {
                var propertyInfo = bindableObject.GetType().GetProperty(Path);
                var getMethod = propertyInfo?.GetGetMethod();
                if (getMethod != null)
                {
                    _internalPropertyGetter = o => getMethod.Invoke(o, null);
                }
            }

            return _internalPropertyGetter;
        }
    }

    private PropertyGetter? _internalPropertyGetter;

    internal PropertySetter? InternalPropertySetter
    {
        get
        {
            if (_internalPropertySetter is not null)
            {
                return _internalPropertySetter;
            }

            _internalPropertySetter ??= PropertySetter;

            if (_internalPropertySetter is null && BindableObjectWeakReference.TryGetTarget(out var bindableObject))
            {
                var propertyInfo = bindableObject.GetType().GetProperty(Path);
                var setMethod = propertyInfo?.GetSetMethod();
                if (setMethod != null)
                {
                    _internalPropertySetter = (o, value) => setMethod.Invoke(o, new object?[] { value });
                }
            }

            return _internalPropertySetter;
        }
    }
    private PropertySetter? _internalPropertySetter;
}

public delegate object? PropertyGetter(object bindableObject);

public delegate void PropertySetter(object bindableObject, object? propertyValue);