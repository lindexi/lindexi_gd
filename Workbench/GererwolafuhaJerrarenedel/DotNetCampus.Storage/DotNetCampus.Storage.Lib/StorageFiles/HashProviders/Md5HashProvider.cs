using System.Security.Cryptography;

namespace DotNetCampus.Storage.StorageFiles;

public class Md5HashProvider() : HashAlgorithmHashProvider(MD5.Create(), StorageHashAlgorithm.Md5);