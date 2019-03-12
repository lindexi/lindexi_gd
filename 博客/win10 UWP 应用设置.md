win10 UWP 应用设置

单的把设置需要的，放到微软自带的LocalSettings

LocalSettings.Values可以存放几乎所有数据

如果需要存放复合数据，一个设置项是由多个值组成，可以使用ApplicationDataCompositeValue将多个合并。

存放一个string

```

string str

{

  set

  {

        ApplicationData.Current.LocalSettings.Values["str"] = value;

  }

  get

  {

            object temp;

            if (ApplicationData.Current.LocalSettings.Values.TryGetValue("width", out temp))

            {

                return  temp as string;

            }

  }

}

```

如果设置在LocalSettings让程序太乱，有很多变量名称一样，可以使用新的ApplicationDataContainer

```

            string str = "";

            var t = ApplicationData.Current.LocalSettings.CreateContainer("str", ApplicationDataCreateDisposition.Always);

            t.Values["str"] = str;

            str = t.Values["str"] as string;

```

http://blog.csdn.net/lindexi_gd