using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Storage.Lib;

public class StorageNode
{
    public StorageNodeType StorageNodeType { get; set; }

    public StorageTextSpan Name { get; set; }

    public StorageTextSpan Value { get; set; } = StorageTextSpan.NullValue;

    public List<StorageNode>? Children { get; set; }
}

