using System.ComponentModel.DataAnnotations;

namespace LibraryApp.Models
{
    public class Author
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string? Name { get; set; }

        // 1 към много: един автор има много книги
        public ICollection<Book>? Books { get; set; }
    }
}