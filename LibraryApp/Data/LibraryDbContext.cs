using LibraryApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data
{
    public class LibraryDbContext : IdentityDbContext<ApplicationUser>
    {
        public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) { }

        public DbSet<Book> Books { get; set; } = null!;
        public DbSet<Author> Authors { get; set; } = null!;
        public DbSet<Member> Members { get; set; } = null!;
        public DbSet<Event> Events { get; set; } = null!;
        public DbSet<EventMember> EventMembers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // --- Relations ---
            b.Entity<EventMember>().HasKey(x => new { x.EventId, x.MemberId });

            b.Entity<EventMember>()
                .HasOne(em => em.Event)
                .WithMany(e => e.EventMembers)
                .HasForeignKey(em => em.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<Book>()
                .HasOne(x => x.Author)
                .WithMany(a => a.Books)
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Seed data (HasData) ---
            b.Entity<Author>().HasData(
                new Author { Id = 1, Name = "Agatha Christie" },
                new Author { Id = 2, Name = "J. R. R. Tolkien" }
            );

            b.Entity<Book>().HasData(
                new Book { Id = 1, Title = "Murder on the Orient Express", ISBN = "9780062693662", Year = 1934, AuthorId = 1 },
                new Book { Id = 2, Title = "The Hobbit", ISBN = "9780547928223", Year = 1937, AuthorId = 2 }
            );

            b.Entity<Member>().HasData(
                new Member { Id = 1, Name = "Alice Johnson", Email = "alice@example.com", JoinedAt = new DateTime(2025, 9, 23) },
                new Member { Id = 2, Name = "Bob Smith", Email = "bob@example.com", JoinedAt = new DateTime(2025, 9, 23) },
                new Member { Id = 3, Name = "Carla Browns", Email = "carla@example.com", JoinedAt = new DateTime(2025, 9, 23) }
            );

            b.Entity<Event>().HasData(
                new Event { Id = 1, Title = "Book Club Meeting", StartAt = new DateTime(2025, 10, 1, 18, 0, 0), Description = "Monthly meetup" }
            );

            b.Entity<EventMember>().HasData(
                new EventMember { EventId = 1, MemberId = 1 },
                new EventMember { EventId = 1, MemberId = 2 }
            );
        }
    }
}
