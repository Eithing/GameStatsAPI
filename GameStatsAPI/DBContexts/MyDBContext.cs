using GameStatsAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStatsAPI.DBContexts
{
    public class MyDBContext : DbContext
    {
        public DbSet<GameActivity> GameActivity { get; set; }

        public MyDBContext(DbContextOptions<MyDBContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GameActivity>().ToTable("activities");

            // Configure PKey
            modelBuilder.Entity<GameActivity>().HasKey(ug => ug.id).HasName("id");

            // Configure indexes  
            modelBuilder.Entity<GameActivity>().HasIndex(p => p.user_id).HasDatabaseName("user_id");
            modelBuilder.Entity<GameActivity>().HasIndex(p => p.game_name).HasDatabaseName("game_name");
            modelBuilder.Entity<GameActivity>().HasIndex(p => p.start_date).HasDatabaseName("start_date");
            modelBuilder.Entity<GameActivity>().HasIndex(p => p.end_date).HasDatabaseName("end_date");

            // Configure columns 
            modelBuilder.Entity<GameActivity>().Property(u => u.user_id).HasColumnType("bigint").IsRequired();
            modelBuilder.Entity<GameActivity>().Property(u => u.game_name).HasColumnType("varchar").HasMaxLength(255).IsRequired();
            modelBuilder.Entity<GameActivity>().Property(u => u.start_date).HasColumnType("datetime").IsRequired();
            modelBuilder.Entity<GameActivity>().Property(u => u.end_date).HasColumnType("datetime").IsRequired();
        }
    }
}
