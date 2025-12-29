namespace DotNetCampus.Storage.StorageFiles;

/// <summary>
/// 存储里用的哈希算法
/// </summary>
/// 正常来说用 MD5 算法就足够了。如果有人听信了 MD5 不安全，那他肯定是不知道为什么 MD5 不安全。~~算了，行行好我多写点注释。在抵抗碰撞的时候 MD5 不安全，但存储里面做的是用来校验文件，本身也不管篡改等事情，此时就无视此问题~~ 越描越乱，就这样吧
/// 额外说明，在现代 CPU 下，算 SHA1 的速度比 MD5 更快，因为有指令集优化
public enum StorageHashAlgorithm
{
    Md5,
    Sha1,
    Sha256,
}