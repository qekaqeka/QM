using DSharpPlus.Lavalink;
using Npgsql;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.EntityFrameworkCore;
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

            var builder = new NpgsqlConnectionStringBuilder();
            builder.Host = "db";
            builder.Username = Environment.GetEnvironmentVariable("POSTGRES_USER");
            builder.Database = Environment.GetEnvironmentVariable("POSTGRES_DB");
            builder.Password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
            builder.Port = 5432;
            optionsBuilder.UseNpgsql(builder.ToString());
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
