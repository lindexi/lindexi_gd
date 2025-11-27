using System.Security.Cryptography;

namespace DotNetCampus.Storage.StorageFiles;

public class Sha256HashProvider() : HashAlgorithmHashProvider(SHA256.Create(), StorageHashAlgorithm.Sha256);