using LibraryApp.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// services
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<LibraryDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// маршрути
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    if (!db.Authors.Any())
    {
        var a1 = new Author { Name = "J. R. R. Tolkien" };
        var a2 = new Author { Name = "Agatha Christie" };
        db.Authors.AddRange(a1, a2);
        db.Books.AddRange(
            new Book { Title = "The Hobbit", ISBN = "9780547928227", Year = 1937, Author = a1 },
            new Book { Title = "Murder on the Orient Express", ISBN = "9780062693662", Year = 1934, Author = a2 }
        );
        await db.SaveChangesAsync();
    }
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    if (!db.Members.Any())
    {
        db.Members.AddRange(
            new Member { FullName = "Alice Johnson", Email = "alice@example.com" },
            new Member { FullName = "Bob Smith", Email = "bob@example.com" },
            new Member { FullName = "Carla Brown", Email = "carla@example.com" }
        );
        await db.SaveChangesAsync();
    }
}

// ВАЖНО: без това приложението ще приключи веднага
app.Run();
