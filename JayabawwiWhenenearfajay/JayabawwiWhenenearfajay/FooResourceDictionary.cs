using System.CodeDom;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace JayabawwiWhenenearfajay;

public class FooResourceDictionary : ResourceDictionary
{
    public FooResourceDictionary()
    {
        Add("SolidColorBrush", this);
    }

    private bool _xx;

    protected override void OnGettingValue(object key, ref object value, out bool canCache)
    {
        if (_xx)
        {
            LoadResourceAndWait();
        }
        else
        {
            bool anyLoad = LoadResource();
            if (!anyLoad)
            {
                LoadResourceAndWait();
            }
        }
        _xx = true;
        //while (App.Task.IsCompleted is false)
        //{
        //    Task.WaitAny(Task.Delay(100), App.Task);
        //}

        var t = App.Current.Resources[key];
        value = t;

        _xx = false;

        Debug.WriteLine(key);
        base.OnGettingValue(key, ref value, out canCache);
    }

    private static void LoadResourceAndWait()
    {
        bool anyLoad = false;
        while (App.Task.IsCompleted is false)
        {
            anyLoad = LoadResource();
            if (anyLoad)
            {
                break;
            }

            Task.WaitAny(Task.Delay(100), App.Task);
        }

        if (!anyLoad)
        {
            LoadResource();
        }
    }

    private static bool LoadResource()
    {
        bool anyLoad = false;
        while (App.Queue.TryDequeue(out var result))
        {
            App.Current.Resources.MergedDictionaries.Add(result);

            anyLoad = true;
        }
        return anyLoad;
    }
}