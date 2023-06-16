using System.CodeDom;
using System.Diagnostics;
using System.Windows;

namespace JayabawwiWhenenearfajay;

public class FooResourceDictionary : ResourceDictionary
{
    public FooResourceDictionary()
    {
        Add("SolidColorBrush", this);
    }

    protected override void OnGettingValue(object key, ref object value, out bool canCache)
    {
        Debug.WriteLine(key);
        base.OnGettingValue(key, ref value, out canCache);
    }
}