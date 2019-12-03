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
            //using (var kekairwuceeYernellijewhebere = new KekairwuceeYernellijewhebere())
            //{
            //    if (kekairwuceeYernellijewhebere.Database.EnsureCreated())
            //    {
            //        kekairwuceeYernellijewhebere.ResourceModel.Add(new ResourceModel()
            //        {
            //            ResourceId = "lindexi",
            //        });
            //        kekairwuceeYernellijewhebere.SaveChanges();
            //    }
            //}

            using (var kekairwuceeYernellijewhebere = new KekairwuceeYernellijewhebere())
            {
                kekairwuceeYernellijewhebere.Database.Migrate();

                kekairwuceeYernellijewhebere.ResourceModel.Add(new ResourceModel()
                {
                    ResourceId = "lindexi",
                });

                foreach (var temp in kekairwuceeYernellijewhebere.ResourceModel)
                {
                    temp.WaircegalhallwayneeHuwairfejaije = "NayardubonaGeqiwiyani";
                    temp.RalellawraFayyelchicurlu = "HelalqejeleNaniherheca";
                    Console.WriteLine(temp.ResourceId);
                }

                kekairwuceeYernellijewhebere.SaveChanges();
            }
        }
    }

    public class ResourceModel
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { set; get; }

        public string ResourceId { set; get; }

        public string WaircegalhallwayneeHuwairfejaije { set; get; }

        public string RalellawraFayyelchicurlu { set; get; }

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
            var file = Path.Combine("FileManger.db");
            file = Path.GetFullPath(file);
            optionsBuilder
                .UseSqlite($"Filename={file}");
        }
    }
}