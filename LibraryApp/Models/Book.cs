using System.ComponentModel.DataAnnotations;

namespace LibraryApp.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string? Title { get; set; }

        [Required, StringLength(13)]
        public string? ISBN { get; set; }

        [Range(1450, 2100)]
        public int Year { get; set; }

        // Връзка към Author (един автор -> много книги)
        public int AuthorId { get; set; }

        public Author? Author { get; set; }

        public string? Summary { get; set; }
    }
}