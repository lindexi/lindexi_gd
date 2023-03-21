using NeedeanarkawFudarkalwi;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
[assembly: MetadataUpdateHandler(typeof(HotReloadMetadataUpdateHandler))]

namespace NeedeanarkawFudarkalwi;
public class HotReloadMetadataUpdateHandler
{
    public static void ClearCache(Type[]? updatedTypes)
    { 
        HotReload++;
    }

    public static void UpdateApplication(Type[]? updatedTypes)
    {
        Console.WriteLine("HotReloadManager.UpdateApplication");
    }

    public static int HotReload { set; get; }
}
