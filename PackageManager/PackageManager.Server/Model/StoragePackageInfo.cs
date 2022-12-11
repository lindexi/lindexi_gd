using System.ComponentModel.DataAnnotations.Schema;

namespace PackageManager.Server.Model;

public class StoragePackageInfo : PackageInfo
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { set; get; }
}