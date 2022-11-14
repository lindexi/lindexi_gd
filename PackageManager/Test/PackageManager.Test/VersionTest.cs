using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;
using PackageManager.Server.Utils;

namespace PackageManager.Test;

[TestClass]
public class VersionTest
{
    [ContractTestCase]
    public void Compare()
    {
        "对比 5.2.0.65535 和 5.2.1.0 版本，可以返回 5.2.1.0 版本更大".Test(() =>
        {
            var version1 = new Version("5.2.0.65535");
            var version2 = new Version("5.2.1.0");

            var versionToLong1 = version1.VersionToLong();
            var versionToLong2 = version2.VersionToLong();

            Assert.AreEqual(true, versionToLong1 < versionToLong2);
        });
    }
}