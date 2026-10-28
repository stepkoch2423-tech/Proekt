using Dapper;
using LibraryDapper.Logic;
using LibraryDapper.Models;
using Npgsql;

namespace LibraryDapper;

public class LibraryRepository
{
    private readonly string _cs;

    public LibraryRepository(string connectionString) => _cs = connectionString;

    private NpgsqlConnection Conn() => new(_cs);

    public async Task<int> AddAuthorAsync(Author author)
    {
        using var c = Conn();
        const string sql = """
            INSERT INTO authors (name, country)
            VALUES (@Name, @Country)
            RETURNING id;
            """;
        return await c.ExecuteScalarAsync<int>(sql, author);
    }

    public async Task<int> AddBookAsync(Book book)
    {
        using var c = Conn();
        const string sql = """
            INSERT INTO books (title, year, author_id)
            VALUES (@Title, @Year, @AuthorId)
            RETURNING id;
            """;
        return await c.ExecuteScalarAsync<int>(sql, book);
    }

    public async Task<int> AddGenreAsync(Genre genre)
    {
        using var c = Conn();
        const string sql = """
            INSERT INTO genres (name)
            VALUES (@Name)
            ON CONFLICT (name) DO UPDATE SET name = EXCLUDED.name
            RETURNING id;
            """;
        return await c.ExecuteScalarAsync<int>(sql, genre);
    }

    public async Task AddGenreToBookAsync(int bookId, int genreId)
    {
        using var c = Conn();
        const string sql = """
            INSERT INTO book_genres (book_id, genre_id)
            VALUES (@bookId, @genreId)
            ON CONFLICT (book_id, genre_id) DO NOTHING;
            """;
        await c.ExecuteAsync(sql, new { bookId, genreId });
    }

    public async Task<IEnumerable<Book>> GetBooksAsync()
    {
        using var c = Conn();
        const string sql = "SELECT id, title, year, author_id FROM books ORDER BY id";
        return (await c.QueryAsync<Book>(sql)).AsList();
    }

    public async Task<IEnumerable<Book>> GetBooksAfterYearAsync(int year)
    {
        using var c = Conn();
        const string sql = """
            SELECT id, title, year, author_id
            FROM books
            WHERE year >= @year
            ORDER BY year, id;
            """;
        return (await c.QueryAsync<Book>(sql, new { year })).AsList();
    }

    public async Task<IEnumerable<Author>> GetAuthorsWithBooksAsync()
    {
        using var c = Conn();
        const string sql = """
            SELECT a.id, a.name, a.country,
                   b.id, b.title, b.year, b.author_id
            FROM authors a
            LEFT JOIN books b ON b.author_id = a.id
            ORDER BY a.id, b.id;
            """;

        var authors = new Dictionary<int, Author>();
        await c.QueryAsync<Author, Book, Author>(
            sql,
            (author, book) =>
            {
                if (!authors.TryGetValue(author.Id, out var existing))
                {
                    existing = author;
                    authors[author.Id] = existing;
                }

                if (book is not null && book.Id != 0)
                    existing.Books.Add(book);

                return existing;
            },
            splitOn: "id");

        return authors.Values;
    }

    public async Task<IEnumerable<Book>> GetBooksWithGenresAsync()
    {
        using var c = Conn();
        const string sql = """
            SELECT id, title, year, author_id FROM books ORDER BY id;
            SELECT id, name FROM genres ORDER BY id;
            SELECT book_id AS BookId, genre_id AS GenreId
            FROM book_genres
            ORDER BY book_id, genre_id;
            """;

        using var multi = await c.QueryMultipleAsync(sql);
        var books = (await multi.ReadAsync<Book>()).AsList();
        var genres = (await multi.ReadAsync<Genre>()).AsList();
        var links = (await multi.ReadAsync<BookGenreLink>()).AsList();

        return BookGenreMapper.AttachGenres(books, genres, links);
    }

    public async Task<IEnumerable<Book>> SearchBooksAsync(string term)
    {
        using var c = Conn();
        const string sql = """
            SELECT id, title, year, author_id
            FROM books
            WHERE title ILIKE @term
            ORDER BY id;
            """;
        return (await c.QueryAsync<Book>(sql, new { term = $"%{term}%" })).AsList();
    }

    // Educational example only: never concatenate untrusted input into SQL.
    public async Task<IEnumerable<Book>> GetBooksUnsafeAsync(string term)
    {
        using var c = Conn();
        var sql = $"""
            SELECT id, title, year, author_id
            FROM books
            WHERE title ILIKE '%{term}%'
            ORDER BY id;
            """;
        return (await c.QueryAsync<Book>(sql)).AsList();
    }

    public async Task<IEnumerable<Book>> GetBooksPagedAsync(int page, int pageSize)
    {
        var (offset, size) = Pagination.GetOffset(page, pageSize);
        using var c = Conn();
        const string sql = """
            SELECT id, title, year, author_id
            FROM books
            ORDER BY id
            LIMIT @size OFFSET @offset;
            """;
        return (await c.QueryAsync<Book>(sql, new { size, offset })).AsList();
    }

    public async Task<bool> TransferBookAsync(int bookId, int newAuthorId)
    {
        using var c = Conn();
        await c.OpenAsync();
        using var transaction = c.BeginTransaction();

        try
        {
            const string sql = """
                UPDATE books
                SET author_id = @newAuthorId
                WHERE id = @bookId;
                """;
            var rows = await c.ExecuteAsync(sql, new { bookId, newAuthorId }, transaction);

            if (rows != 1)
            {
                transaction.Rollback();
                return false;
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch
            {
                // Preserve the original database exception.
            }

            throw;
        }
    }

    public async Task<IEnumerable<Book>> GetBooksByAuthorFunctionAsync(int authorId)
    {
        using var c = Conn();
        const string sql = "SELECT * FROM get_books_by_author(@AuthorId);";
        return (await c.QueryAsync<Book>(sql, new { AuthorId = authorId })).AsList();
    }

    public async Task<bool> UpdateBookYearAsync(int bookId, int newYear)
    {
        using var c = Conn();
        const string sql = "UPDATE books SET year = @newYear WHERE id = @bookId";
        var rows = await c.ExecuteAsync(sql, new { bookId, newYear });
        return rows > 0;
    }

    public async Task<bool> DeleteAuthorAsync(int authorId)
    {
        using var c = Conn();
        var rows = await c.ExecuteAsync(
            "DELETE FROM authors WHERE id = @authorId",
            new { authorId });
        return rows > 0;
    }
}
