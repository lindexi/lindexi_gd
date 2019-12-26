using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KeahelnawwalyoNelwerchaje;

namespace KeahelnawwalyoNelwerchaje.Models
{
    public class NodeContext : DbContext
    {
        public NodeContext (DbContextOptions<NodeContext> options)
            : base(options)
        {
        }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Node>().HasIndex(temp => temp.MainIp);

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<KeahelnawwalyoNelwerchaje.Node> Node { get; set; }
    }
}
