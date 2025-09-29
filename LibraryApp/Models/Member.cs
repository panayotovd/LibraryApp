using System.ComponentModel.DataAnnotations;

namespace LibraryApp.Models
{
    public class Member
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string? Name { get; set; }

        [EmailAddress, Required]
        public string? Email { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public List<EventMember> EventMembers { get; set; } = [];
    }
}