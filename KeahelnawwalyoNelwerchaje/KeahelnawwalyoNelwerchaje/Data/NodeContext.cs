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

        public DbSet<KeahelnawwalyoNelwerchaje.Node> Node { get; set; }
    }
}
