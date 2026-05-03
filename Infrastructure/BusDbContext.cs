using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Domain.Entities;
using System.Reflection;

namespace Infrastructure
{
    public class BusDbContext : DbContext
    {
        public DbSet<StopPlace> StopPlaces { get; set; }

        public BusDbContext(DbContextOptions options) : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
