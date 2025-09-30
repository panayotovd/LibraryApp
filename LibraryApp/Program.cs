using LibraryApp.Data;
using LibraryApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services
    .AddDefaultIdentity<ApplicationUser>(o => { o.SignIn.RequireConfirmedAccount = false; })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<LibraryDbContext>();

// Authorization policy: CanWrite = Admin или Librarian
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanWrite", policy => policy.RequireRole("Admin", "Librarian"));
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// seed роли и демо админ, ако съществува user admin@library.local
await EnsureRolesAndAdminAsync(app);

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
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    await IdentitySeed.RunAsync(scope);
}

app.Run();

// ===== helpers =====
static async Task EnsureRolesAndAdminAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    foreach (var r in new[] { "Admin", "Librarian" })
        if (!await roles.RoleExistsAsync(r)) await roles.CreateAsync(new IdentityRole(r));

    // ако имаш потребител admin@library.local, сложи му роля Admin
    var admin = await users.FindByEmailAsync("admin@library.local");
    if (admin != null && !await users.IsInRoleAsync(admin, "Admin"))
        await users.AddToRoleAsync(admin, "Admin");
}
