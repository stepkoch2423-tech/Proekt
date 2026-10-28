using LibraryDapper.Logic;
using LibraryDapper.Models;

namespace LibraryDapper.Tests;

public class LibraryLogicTests
{
    [Fact]
    public void GetOffset_uses_zero_based_page_index()
    {
        Assert.Equal((10, 10), Pagination.GetOffset(page: 2, pageSize: 10));
    }

    [Fact]
    public void AttachGenres_maps_link_rows_to_books()
    {
        var books = new[] { new Book { Id = 1, Title = "1984" } };
        var genres = new[] { new Genre { Id = 7, Name = "Dystopia" } };
        var links = new[] { new BookGenreLink { BookId = 1, GenreId = 7 } };

        var result = BookGenreMapper.AttachGenres(books, genres, links).Single();

        Assert.Equal("Dystopia", Assert.Single(result.Genres).Name);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(-1, 10)]
    public void GetOffset_rejects_non_positive_values(int page, int pageSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Pagination.GetOffset(page, pageSize));
    }

    [Fact]
    public void AttachGenres_ignores_unknown_links_and_duplicate_rows()
    {
        var books = new[] { new Book { Id = 1, Title = "The Hobbit" } };
        var genres = new[] { new Genre { Id = 2, Name = "Fantasy" } };
        var links = new[]
        {
            new BookGenreLink { BookId = 1, GenreId = 2 },
            new BookGenreLink { BookId = 1, GenreId = 2 },
            new BookGenreLink { BookId = 99, GenreId = 2 },
            new BookGenreLink { BookId = 1, GenreId = 99 }
        };

        var result = BookGenreMapper.AttachGenres(books, genres, links).Single();

        Assert.Single(result.Genres);
        Assert.Single(genres.Single().Books);
    }
}
