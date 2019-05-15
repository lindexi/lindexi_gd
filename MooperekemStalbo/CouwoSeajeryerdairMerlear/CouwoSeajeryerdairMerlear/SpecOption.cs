using CommandLine;

namespace CouwoSeajeryerdairMerlear
{
    /// <summary>
    /// 创建打包文件
    /// </summary>
    [Verb("spec")]
    public class SpecOption
    {
        [Option("package-id")]
        public string PackageID { set; get; }
    }
}