using System.IO;
using System.Security.Cryptography;

namespace DotNetCampus.Storage.StorageFiles;

public abstract class HashAlgorithmHashProvider : IHashProvider
{
    protected HashAlgorithmHashProvider(HashAlgorithm hashAlgorithm, StorageHashAlgorithm storageHashAlgorithm)
    {
        HashAlgorithm = hashAlgorithm;
        StorageHashAlgorithm = storageHashAlgorithm;
    }

    public HashAlgorithm HashAlgorithm { get; }
    public StorageHashAlgorithm StorageHashAlgorithm { get; }

    public async ValueTask<HashResult> ComputeHashAsync(Stream inputStream)
    {
        var hashBytes = await HashAlgorithm.ComputeHashAsync(inputStream);
        var hashValue = string.Join("", hashBytes.Select(b => b.ToString("x2")));
        return new HashResult(hashValue, GetStorageHashAlgorithmText());
    }

    private string GetStorageHashAlgorithmText()
    {
        // 作用类似 StorageHashAlgorithm.ToString() 的功能。只是因为担心 AOT 去掉源数据丢失枚举字符串
        return StorageHashAlgorithm switch
        {
            StorageHashAlgorithm.Md5 => nameof(StorageHashAlgorithm.Md5),
            StorageHashAlgorithm.Sha1 => nameof(StorageHashAlgorithm.Sha1),
            StorageHashAlgorithm.Sha256 => nameof(StorageHashAlgorithm.Sha256),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}