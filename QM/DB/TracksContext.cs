using DSharpPlus.Lavalink;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection.Emit;


namespace QM.DB
{
    public class TracksContext : DbContext
    {
        public TracksContext() => Database.EnsureCreated();

        public TracksContext(DbContextOptions<TracksContext> options)
          : base(options)
        {
        }

        public virtual DbSet<Track> Tracks { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlite("Data Source=.\\DB\\Tracks.db");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Track>().
                HasKey(t => t.Id);

            modelBuilder.Entity<Track>().
                Property(t => t.LavalinkTrack).
                HasColumnName("TrackString").
                HasConversion<string>(
                    t => t.TrackString, 
                    t => LavalinkUtilities.DecodeTrack(t) ?? new LavalinkTrack());
        }
    }
}
