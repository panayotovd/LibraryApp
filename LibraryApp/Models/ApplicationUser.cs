using Microsoft.AspNetCore.Identity;

namespace LibraryApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        // по желание: публично име и т.н.
        public string? DisplayName { get; set; }
    }
}
