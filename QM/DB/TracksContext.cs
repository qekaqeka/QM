using DSharpPlus.Lavalink;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection.Emit;


namespace QM.DB
{
    public class TracksContext : DbContext
    {
        public TracksContext()
        {
            try
            {
                bool t = Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public TracksContext(DbContextOptions<TracksContext> options)
          : base(options)
        {
        }

        public virtual DbSet<Track> Tracks { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("secrets.json", true, true)
                .Build();

            optionsBuilder.UseSqlite(config["QM:ConnectionString"]);
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Track>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<Track>()
                .Property(t => t.LavalinkTrack)
                .HasColumnName("TrackString")
                .HasConversion<string>(
                    t => t.TrackString, 
                    t => LavalinkUtilities.DecodeTrack(t) ?? new LavalinkTrack());
        }
    }
}
