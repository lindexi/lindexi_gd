using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MooperekemStalbo;

namespace MooperekemStalbo.Models
{
    public class MooperekemStalboContext : DbContext
    {
        public MooperekemStalboContext (DbContextOptions<MooperekemStalboContext> options)
            : base(options)
        {
        }

        public DbSet<MooperekemStalbo.GairKetemRairsem> GairKetemRairsem { get; set; }
    }
}
