using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FayawlerjiraywereNiharjeechel.Controllers;

namespace FayawlerjiraywereNiharjeechel.Models
{
    public class FayawlerjiraywereNiharjeechelContext : DbContext
    {
        public FayawlerjiraywereNiharjeechelContext (DbContextOptions<FayawlerjiraywereNiharjeechelContext> options)
            : base(options)
        {
        }

        public DbSet<FayawlerjiraywereNiharjeechel.Controllers.VisitingCount> VisitingCount { get; set; }
    }
}
