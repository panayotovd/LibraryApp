using System.ComponentModel.DataAnnotations;

namespace LibraryApp.Models.ViewModels
{
    public class EventFormViewModel
    {
        public int? Id { get; set; }

        [Required, StringLength(150)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        [Required]
        public DateTime StartAt { get; set; }

        // за много-към-много
        public List<int> SelectedMemberIds { get; set; } = [];

        // за рендване в изгледа
        public IEnumerable<Member> AllMembers { get; set; } = [];
    }
}
