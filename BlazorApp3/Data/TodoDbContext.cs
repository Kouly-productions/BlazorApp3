using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Data
{
    public class TodoDbContext : DbContext
    {
        public TodoDbContext(DbContextOptions<TodoDbContext> options)
            : base(options)
        {
        }

        public DbSet<CprRecord> CprRecords { get; set; }
        public DbSet<TodoItem> TodoItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Create indexes for faster lookups
            modelBuilder.Entity<CprRecord>()
                .HasIndex(c => c.UserId);

            modelBuilder.Entity<TodoItem>()
                .HasIndex(t => t.UserId);
        }
    }
}