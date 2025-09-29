using LibraryApp.Models;
using Microsoft.AspNetCore.Identity;

namespace LibraryApp.Data
{
    public static class IdentitySeed
    {
        public static async Task RunAsync(IServiceScope scope)
        {
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // роли
            foreach (var r in new[] { "Admin", "Librarian" })
                if (!await roleMgr.RoleExistsAsync(r))
                    await roleMgr.CreateAsync(new IdentityRole(r));

            // настройки за пароли от appsettings (fallback към дефолт)
            string adminEmail = config["Seed:Admin:Email"] ?? "admin@library.local";
            string adminPass = config["Seed:Admin:Password"] ?? "Admin123!";
            string libEmail = config["Seed:Librarian:Email"] ?? "librarian@library.local";
            string libPass = config["Seed:Librarian:Password"] ?? "Librarian123!";

            // админ
            var admin = await userMgr.FindByEmailAsync(adminEmail);
            if (admin is null)
            {
                admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true, DisplayName = "Administrator" };
                if ((await userMgr.CreateAsync(admin, adminPass)).Succeeded)
                    await userMgr.AddToRoleAsync(admin, "Admin");
            }

            // библиотекар
            var lib = await userMgr.FindByEmailAsync(libEmail);
            if (lib is null)
            {
                lib = new ApplicationUser { UserName = libEmail, Email = libEmail, EmailConfirmed = true, DisplayName = "Librarian" };
                if ((await userMgr.CreateAsync(lib, libPass)).Succeeded)
                    await userMgr.AddToRoleAsync(lib, "Librarian");
            }
        }
    }
}
