using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace HalelkeherCiwerecawqal
{
    class Program
    {
        static void Main(string[] args)
        {
            
        }
    }

    public class ResourceModel
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] 
        public string Id { set; get; }

        public string ResourceId { set; get; }

        public string ResourceName { set; get; }

        public string LocalPath { set; get; }

        public string ResourceSign { set; get; }

        public string ResourceFileDetail { set; get; }
    }

    public class KekairwuceeYernellijewhebere : DbContext
    {
        public DbSet<ResourceModel> ResourceModel { get; set; }

        /// <inheritdoc />
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var file = Path.Combine( "FileManger.db");
            file = Path.GetFullPath(file);
            optionsBuilder
                .UseSqlite($"Filename={file}");
        }
    }
}
