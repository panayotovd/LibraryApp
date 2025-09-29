using LibraryApp.Data;
using LibraryApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DbContext (SQL Server) — взима "DefaultConnection" от appsettings.json
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity (Users + Roles) върху нашия DbContext
builder.Services
    .AddDefaultIdentity<ApplicationUser>(o =>
    {
        o.SignIn.RequireConfirmedAccount = false;
        o.Password.RequiredLength = 6;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequireUppercase = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<LibraryDbContext>();

builder.Services.AddControllersWithViews();

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("CanWrite", p => p.RequireRole("Admin", "Librarian"));
});

var app = builder.Build();

// Apply pending migrations + seed на роли/админ
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    if (db.Database.GetPendingMigrations().Any())
        db.Database.Migrate();

    // ако имаш клас Data/IdentitySeed.cs с RunAsync(scope)
    await IdentitySeed.RunAsync(scope);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // за Identity UI (Login/Register)

app.Run();
