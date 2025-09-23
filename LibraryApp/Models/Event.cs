using System.ComponentModel.DataAnnotations;

namespace LibraryApp.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        [Required]
        public DateTime StartAt { get; set; }

        // много към много: събитие <-> членове
        public List<EventMember>? EventMembers { get; set; } = [];
    }
}