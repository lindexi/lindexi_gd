using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WatchDagServer.Model;

namespace WatchDagServer.Data
{
    public class WatchDagServerContext : DbContext
    {
        public WatchDagServerContext (DbContextOptions<WatchDagServerContext> options)
            : base(options)
        {
        }

        public DbSet<WatchDagServer.Model.RegisterContext> RegisterContext { get; set; } = null!;
    }
}
