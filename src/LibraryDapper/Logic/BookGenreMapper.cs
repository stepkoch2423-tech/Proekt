using LibraryDapper.Models;

namespace LibraryDapper.Logic;

public static class BookGenreMapper
{
    public static IReadOnlyList<Book> AttachGenres(
        IEnumerable<Book> books,
        IEnumerable<Genre> genres,
        IEnumerable<BookGenreLink> links)
    {
        var bookList = books.ToList();
        var genreById = genres.ToDictionary(genre => genre.Id);
        var bookById = bookList.ToDictionary(book => book.Id);

        foreach (var book in bookList)
            book.Genres.Clear();

        foreach (var genre in genreById.Values)
            genre.Books.Clear();

        foreach (var link in links)
        {
            if (!bookById.TryGetValue(link.BookId, out var book) ||
                !genreById.TryGetValue(link.GenreId, out var genre))
                continue;

            if (!book.Genres.Any(existing => existing.Id == genre.Id))
            {
                book.Genres.Add(genre);
                genre.Books.Add(book);
            }
        }

        return bookList;
    }
}
