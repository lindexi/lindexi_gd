namespace BefawafereKehufallkee;

public readonly record struct ClrBindingPropertyContext(WeakReference<object> BindableObjectWeakReference, string Path, Func<object>? PropertyGetter,
    Action<object>? PropertySetter)
{
}