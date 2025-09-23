using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Models
{
    public class LibraryDbContext(DbContextOptions<LibraryDbContext> options) : DbContext(options)
    {
        public DbSet<Book> Books { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventMember> EventMembers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // join таблица (many-to-many)
            modelBuilder.Entity<EventMember>()
                .HasKey(em => new { em.EventId, em.MemberId });

            modelBuilder.Entity<EventMember>()
                .HasOne(em => em.Event)
                .WithMany(e => e.EventMembers)
                .HasForeignKey(em => em.EventId);

            modelBuilder.Entity<EventMember>()
                .HasOne(em => em.Member)
                .WithMany()
                .HasForeignKey(em => em.MemberId);
        }
    }
}