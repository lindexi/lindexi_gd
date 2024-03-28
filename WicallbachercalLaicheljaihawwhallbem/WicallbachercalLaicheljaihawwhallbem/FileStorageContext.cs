using Microsoft.EntityFrameworkCore;

namespace WicallbachercalLaicheljaihawwhallbem;

public class FileStorageContext : DbContext
{
    public FileStorageContext()
    {
        // 用于设计时
        _sqliteFile = "FileManger.db";
    }

    public FileStorageContext(string sqliteFile)
    {
        _sqliteFile = sqliteFile;
    }

    private readonly string _sqliteFile;

    public DbSet<FileStorageModel> FileStorageModel { set; get; } = null!;
    public DbSet<FileRecordModel> FileRecordModel { set; get; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSqlite($"Filename={_sqliteFile}");
    }
}