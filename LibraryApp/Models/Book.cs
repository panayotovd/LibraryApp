using System.ComponentModel.DataAnnotations;

namespace LibraryApp.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string? Title { get; set; }

        [Range(0, 2100)]
        public int Year { get; set; }

        public int AuthorId { get; set; }

        public Author? Author { get; set; }
    }
}