using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Storage.Serialization;

/// <summary>
/// 使用 Open Packaging Conventions(OPC) 格式的序列化器
/// </summary>
/// https://learn.microsoft.com/en-us/previous-versions/windows/desktop/opc/open-packaging-conventions-overview
/// > The ECMA-376 OpenXML, 1st Edition, Part 2: Open Packaging Conventions (OPC) can be more easily understood through an analogy with real world filing systems. 
public class OpcSerializer
{
    public OpcSerializer(DirectoryInfo? workingDirectoryInfo = null)
    {
        WorkingDirectoryInfo = workingDirectoryInfo ??
                               new DirectoryInfo(Path.Join(Path.GetTempPath(), Path.GetRandomFileName())); ;
    }

    public DirectoryInfo WorkingDirectoryInfo { get; }


}