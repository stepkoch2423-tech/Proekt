namespace LibraryDapper.Models;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public int? Year { get; set; }
    public int AuthorId { get; set; }
    public List<Genre> Genres { get; set; } = new();
}
