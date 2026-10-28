# Library Dapper Homework Design

## Goal

Create a small .NET 8 console application that demonstrates Dapper with PostgreSQL for a library domain. The application must cover authors, books, genres with a many-to-many relationship, QueryMultiple mapping, parameterized search, pagination, a transaction, SQL injection contrast, and a PostgreSQL function call.

## Architecture

The project will use one console application with a small repository boundary. `LibraryRepository` owns connection creation and SQL for authors, books, genres, and the book-genre link table. `schema.sql` is the single database setup script and is safe to run on a new `library` database.

The console entry point seeds deterministic sample data and prints each requested demonstration. Database credentials will be configurable through `ConnectionStrings__Library` with the assignment connection string as the documented default. No ORM or web framework will be introduced.

## Database model

- `authors(id, name, country)` stores authors.
- `books(id, title, year, author_id)` stores books and references authors with `ON DELETE CASCADE`.
- `genres(id, name)` stores unique genre names.
- `book_genres(book_id, genre_id)` is the many-to-many link table with a composite primary key and cascading foreign keys.
- `get_books_by_author(p_author_id)` is a PostgreSQL table-valued function returning books for one author.

## Repository API

`LibraryRepository` will expose the original CRUD methods plus:

- `Task<int> AddGenreAsync(Genre genre)`
- `Task AddGenreToBookAsync(int bookId, int genreId)`
- `Task<IEnumerable<Book>> GetBooksWithGenresAsync()`
- `Task<IEnumerable<Book>> SearchBooksAsync(string term)` using `ILIKE @term`.
- `Task<IEnumerable<Book>> GetBooksPagedAsync(int page, int pageSize)` using `LIMIT @size OFFSET @offset`.
- `Task<bool> TransferBookAsync(int bookId, int newAuthorId)` using `BeginTransaction()` and returning whether one row was updated.
- `Task<IEnumerable<Book>> GetBooksByAuthorFunctionAsync(int authorId)` using `SELECT * FROM get_books_by_author(@AuthorId)`.
- `Task<IEnumerable<Book>> GetBooksUnsafeAsync(string term)` as an explicitly labeled educational example that concatenates input into SQL.

`GetBooksWithGenresAsync` will issue three result sets in one command: books, genres, and links. The method will use Dapper `QueryMultipleAsync`, index genres by id, and attach genres to books through the link rows. The unsafe method will be isolated beside the safe method and documented only for demonstration; normal application code will use parameters.

## Data models

`Author` and `Book` will retain the assignment properties. `Book` will gain `List<Genre> Genres`, and `Genre` will contain `Id`, `Name`, and `List<Book> Books` for the many-to-many model. A private link DTO will represent `book_genres` rows without exposing database-only plumbing in the public model API.

## Console demonstration

`Program.cs` will:

1. Apply the SQL script only through documented setup instructions, not by silently mutating schema at startup.
2. Insert authors, books, genres, and book-genre links.
3. Print books with genres through `QueryMultiple`.
4. Print a search result and a paged result.
5. Transfer a book to another author and print the result.
6. Call the PostgreSQL function through Dapper.
7. Run the unsafe and safe search examples with a payload such as `%' OR 1=1 --`, explaining that the concatenated query changes SQL while the parameterized query treats it as data.

## Error handling and validation

Page numbers and page sizes must be positive; invalid values throw `ArgumentOutOfRangeException`. The transaction updates exactly one book row and commits only on success; exceptions roll back and are rethrown. PostgreSQL constraint errors are allowed to surface to the caller with their original details.

## Testing and verification

The repository will have focused tests for pagination offset calculation, SQL parameter usage, and model mapping where they can be tested without a live database. A live PostgreSQL run is documented separately and is used when the local database is available. Verification will include `dotnet build`, `dotnet test`, SQL script review, and a clean Git diff before pushing to the supplied GitHub repository.

## Scope exclusions

No UI, authentication, migrations framework, Docker requirement, or production-grade secret management will be added. The assignment remains a console/Dapper exercise with a clear educational SQL-injection example.
