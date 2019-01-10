using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FojirCacarcallCeqoChecer.Model;

namespace FojirCacarcallCeqoChecer.Models
{
    public class RasasCootrorvouContext : DbContext
    {
        public RasasCootrorvouContext (DbContextOptions<RasasCootrorvouContext> options)
            : base(options)
        {
        }

        public DbSet<FojirCacarcallCeqoChecer.Model.PerekallbearvirFemheakorTirtaKema> PerekallbearvirFemheakorTirtaKema { get; set; }


    }
}
