using System.Security.Cryptography;

namespace DotNetCampus.Storage.StorageFiles;

public class Sha1HashProvider() : HashAlgorithmHashProvider(SHA1.Create(), StorageHashAlgorithm.Sha1);