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
    public List<SaveInfo>? SaveInfoList { get; set; }

    [SaveInfoMember("Count")]
    public List<object>? CountList { get; set; }

    [SaveInfoMember("F2")]
    public FooEnum Foo2Enum { get; set; }
}

public enum FooEnum
{
    Value1,
    Value2,
    Value3
}