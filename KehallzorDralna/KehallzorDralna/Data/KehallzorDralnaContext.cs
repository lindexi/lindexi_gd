using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KehallzorDralna.Model;

namespace KehallzorDralna.Models
{
    public class KehallzorDralnaContext : DbContext
    {
        public KehallzorDralnaContext (DbContextOptions<KehallzorDralnaContext> options)
            : base(options)
        {
        }

        public DbSet<KehallzorDralna.Model.XaseYinairtraiSeawhallkou> XaseYinairtraiSeawhallkou { get; set; }
    }
}
