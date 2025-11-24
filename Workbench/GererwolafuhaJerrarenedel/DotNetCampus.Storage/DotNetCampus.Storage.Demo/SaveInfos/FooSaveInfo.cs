using DotNetCampus.Storage.Lib.SaveInfos;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Storage.Demo.SaveInfos;

[SaveInfoContract("Foo")]
public class FooSaveInfo : SaveInfo
{
    [SaveInfoMember("FooProperty", Description = "This is a foo property.")]
    public int FooProperty { get; set; } = 2;

    [SaveInfoMember("F1", Description = "This is a foo property.")]
    public Foo1SaveInfo? Foo1 { get; set; }

    [SaveInfoMember("S1")]
    public IList<SaveInfo>? SaveInfoList { get; set; }
}