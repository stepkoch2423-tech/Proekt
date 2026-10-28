# Library Dapper Homework Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build and verify a .NET 8 Dapper/PostgreSQL console project that demonstrates genres, QueryMultiple, search, pagination, transactions, SQL injection, and a PostgreSQL function.

**Architecture:** One console project contains models, pure mapping/validation helpers, and `LibraryRepository` for all PostgreSQL access. A separate xUnit project tests deterministic pagination and many-to-many mapping without requiring a running database; `schema.sql` and README cover live PostgreSQL setup.

**Tech Stack:** .NET 8, C#, Dapper 2.x, Npgsql 8.x, xUnit 2.x, PostgreSQL.

**Spec:** `docs/superpowers/specs/2026-10-28-library-dapper-design.md`

## Global Constraints

- The project is a .NET 8 console application.
- `GetBooksWithGenresAsync` must use Dapper `QueryMultipleAsync` with books, genres, and links as separate result sets.
- Search must use `ILIKE @term`; unsafe concatenation is educational-only and explicitly labeled.
- Pagination must use `LIMIT @size OFFSET @offset` and reject non-positive page values.
- Transfers must use `BeginTransaction()` and commit only after a successful update.
- The PostgreSQL function must be called with `SELECT * FROM get_books_by_author(@AuthorId)`.
- No UI, authentication, migrations framework, Docker requirement, or production secret-management system is in scope.

---

### Task 1: Scaffold the solution and prove pure domain behavior with TDD

**Files:**
- Create: `LibraryDapper.slnx`
- Create: `src/LibraryDapper/LibraryDapper.csproj`
- Create: `src/LibraryDapper/Models/Author.cs`
- Create: `src/LibraryDapper/Models/Book.cs`
- Create: `src/LibraryDapper/Models/Genre.cs`
- Create: `src/LibraryDapper/Models/BookGenreLink.cs`
- Create: `src/LibraryDapper/Logic/Pagination.cs`
- Create: `src/LibraryDapper/Logic/BookGenreMapper.cs`
- Create: `tests/LibraryDapper.Tests/LibraryDapper.Tests.csproj`
- Create: `tests/LibraryDapper.Tests/LibraryLogicTests.cs`

**Interfaces:**
- `Pagination.GetOffset(int page, int pageSize)` returns `(int offset, int size)` and throws `ArgumentOutOfRangeException` for non-positive inputs.
- `BookGenreMapper.AttachGenres(IEnumerable<Book> books, IEnumerable<Genre> genres, IEnumerable<BookGenreLink> links)` returns books with matching `Genres` lists.
- `BookGenreLink` contains `BookId` and `GenreId`.

- [ ] **Step 1: Create only the project files and the failing tests.**

```csharp
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
```

- [ ] **Step 2: Run the tests and verify the expected RED state.**

Run: `dotnet test tests/LibraryDapper.Tests/LibraryDapper.Tests.csproj`

Expected: compilation failure because `Pagination`, `Book`, `Genre`, `BookGenreLink`, and `BookGenreMapper` do not exist yet.

- [ ] **Step 3: Add the minimal models and helpers.**

```csharp
public static (int Offset, int Size) GetOffset(int page, int pageSize)
{
    if (page <= 0) throw new ArgumentOutOfRangeException(nameof(page));
    if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));
    return checked(((page - 1) * pageSize, pageSize));
}
```

`Book` has `Id`, `Title`, nullable `Year`, `AuthorId`, and `List<Genre> Genres`; `Genre` has `Id`, `Name`, and `List<Book> Books`; `Author` retains `Books`.

- [ ] **Step 4: Run the focused tests and verify GREEN.**

Run: `dotnet test tests/LibraryDapper.Tests/LibraryDapper.Tests.csproj`

Expected: all tests pass with zero failures.

- [ ] **Step 5: Commit the scaffold and pure logic.**

```bash
git add LibraryDapper.sln src tests
git commit -m "feat: scaffold library domain and pure helpers"
```

### Task 2: Add the PostgreSQL schema and repository API

**Files:**
- Create: `src/LibraryDapper/schema.sql`
- Create: `src/LibraryDapper/LibraryRepository.cs`
- Create: `src/LibraryDapper/Models/BookWithAuthor.cs` if needed for function results
- Modify: `src/LibraryDapper/Logic/Pagination.cs`
- Modify: `src/LibraryDapper/LibraryDapper.csproj`

**Interfaces:**
- `LibraryRepository(string connectionString)` owns an `NpgsqlConnection` per operation.
- Existing assignment methods remain: `AddAuthorAsync`, `AddBookAsync`, `GetBooksAsync`, `GetBooksAfterYearAsync`, `GetAuthorsWithBooksAsync`, `UpdateBookYearAsync`, and `DeleteAuthorAsync`.
- New methods are exactly the signatures in the design spec.

- [ ] **Step 1: Add Dapper/Npgsql package references and a schema test fixture file.**

Use package references `Dapper` 2.x and `Npgsql` 8.x in the application; use `xunit`, `xunit.runner.visualstudio`, and `Microsoft.NET.Test.Sdk` in tests.

- [ ] **Step 2: Write the schema before repository SQL.**

`schema.sql` must create `authors`, `books`, `genres`, and `book_genres` with the specified keys and constraints, then create:

```sql
CREATE OR REPLACE FUNCTION get_books_by_author(p_author_id INT)
RETURNS TABLE (id INT, title VARCHAR(200), year INT, author_id INT)
LANGUAGE SQL
AS $$
    SELECT b.id, b.title, b.year, b.author_id
    FROM books b
    WHERE b.author_id = p_author_id
    ORDER BY b.id;
$$;
```

- [ ] **Step 3: Implement CRUD, genre commands, QueryMultiple, search, pagination, transaction, and function call.**

The QueryMultiple method must execute one command shaped as:

```sql
SELECT id, title, year, author_id FROM books ORDER BY id;
SELECT id, name FROM genres ORDER BY id;
SELECT book_id AS BookId, genre_id AS GenreId FROM book_genres ORDER BY book_id, genre_id;
```

The safe search must be:

```sql
SELECT id, title, year, author_id
FROM books
WHERE title ILIKE @term
ORDER BY id;
```

The paged query must use `LIMIT @size OFFSET @offset`, with values from `Pagination.GetOffset`. `TransferBookAsync` must open a transaction, execute `UPDATE books SET author_id = @newAuthorId WHERE id = @bookId`, commit when the command completes, and roll back on exception.

- [ ] **Step 4: Add focused tests for validation and mapping edge cases, then run them.**

Test `page <= 0`, `pageSize <= 0`, an empty link set, and a link to a missing genre. The mapper must ignore links whose book or genre is absent and must not throw.

Run: `dotnet test`

Expected: all tests pass.

- [ ] **Step 5: Commit schema and repository.**

```bash
git add src tests
git commit -m "feat: add postgres schema and dapper repository"
```

### Task 3: Add the educational console demo and documentation

**Files:**
- Create: `src/LibraryDapper/Program.cs`
- Create: `README.md`
- Create: `.gitignore`

**Interfaces:**
- `Program.cs` reads `ConnectionStrings__Library`, falling back to `Host=localhost;Port=5432;Database=library;Username=lab;Password=lab123`.
- The console output demonstrates every required operation and labels unsafe SQL as intentionally vulnerable.

- [ ] **Step 1: Add the failing smoke expectation to the documentation checklist.**

Document the exact setup commands:

```bash
createdb -h localhost -U lab library
psql -h localhost -U lab -d library -f src/LibraryDapper/schema.sql
dotnet run --project src/LibraryDapper/LibraryDapper.csproj
```

- [ ] **Step 2: Implement the minimal deterministic demo.**

Seed Tolkien, Orwell, three books, and genres such as `Fantasy`, `Classic`, and `Dystopia`; print QueryMultiple results, search, page 1, a transfer, the function result, and both injection examples. `GetBooksUnsafeAsync` must be visibly marked `UNSAFE` in code comments and README.

- [ ] **Step 3: Document the injection contrast.**

Show that concatenating `%' OR 1=1 --` changes SQL structure, while `ILIKE @term` sends the same text as a parameter. State that the unsafe method exists only for the lesson and must never be used with user input.

- [ ] **Step 4: Run build and tests.**

Run: `dotnet build` and `dotnet test`

Expected: both exit with code 0. If PostgreSQL is available, also run the schema and console commands and capture the output in the final report.

- [ ] **Step 5: Commit the console demo and documentation.**

```bash
git add .gitignore README.md src/LibraryDapper/Program.cs
git commit -m "feat: demonstrate dapper homework scenarios"
```

### Task 4: Final verification and GitHub submission

**Files:**
- Modify only files needed to fix verification findings.

- [ ] **Step 1: Inspect the final diff and run the complete checks.**

```bash
git diff --check HEAD~3..HEAD
dotnet test
dotnet build --configuration Release
git status --short
```

- [ ] **Step 2: Verify requirement coverage.**

Confirm by inspection that `schema.sql` contains `genres` and `book_genres`, the repository contains `QueryMultipleAsync`, `ILIKE @term`, `LIMIT @size OFFSET @offset`, `BeginTransaction`, and `SELECT * FROM get_books_by_author(@AuthorId)`, and README explains SQL injection.

- [ ] **Step 3: Push the commits to the supplied repository.**

```bash
git push -u origin main
```

If GitHub authentication is unavailable, leave the verified commits local and report the exact push error instead of rewriting history.
