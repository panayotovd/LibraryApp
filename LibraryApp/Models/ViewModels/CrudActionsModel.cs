namespace LibraryApp.Models.ViewModels
{
    public class CrudActionsModel
    {
        public string Controller { get; set; } = "";
        public int Id { get; set; }
        // по желание: override на текстовете
        public string DetailsText { get; set; } = "Детайли";
        public string EditText { get; set; } = "Редакция";
        public string DeleteText { get; set; } = "Изтриване";
    }
}
