namespace DotNetCampus.Storage.Standard;

internal static class UriUtils
{
    /// <summary>
    /// 去掉值的前缀
    /// </summary>
    /// <param name="value"></param>
    /// <param name="prefix"></param>
    /// <returns></returns>
    public static string RemovePrefix(string value, string prefix)
    {
        // 下面代码解决 value = " id://foo" 的问题
        value = value.Trim();

        // 前缀是内部传入的，确保不需要删除空格
        //prefix = prefix.Trim();

        // 忽略大小写 协议应该都不会区分大小写
        if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return value.Substring(prefix.Length).Trim();
        }

        return value;
    }
}