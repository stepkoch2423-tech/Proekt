using LibraryDapper;
using LibraryDapper.Models;

const string defaultConnectionString =
    "Host=localhost;Port=5432;Database=library;Username=lab;Password=lab123";
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Library")
                       ?? defaultConnectionString;

var repo = new LibraryRepository(connectionString);

try
{
    var tolkienId = await repo.AddAuthorAsync(new Author
    {
        Name = "J.R.R. Tolkien",
        Country = "UK"
    });
    var hobbitId = await repo.AddBookAsync(new Book
    {
        Title = "The Hobbit",
        Year = 1937,
        AuthorId = tolkienId
    });
    var fellowshipId = await repo.AddBookAsync(new Book
    {
        Title = "The Fellowship of the Ring",
        Year = 1954,
        AuthorId = tolkienId
    });

    var orwellId = await repo.AddAuthorAsync(new Author
    {
        Name = "George Orwell",
        Country = "UK"
    });
    var book1984Id = await repo.AddBookAsync(new Book
    {
        Title = "1984",
        Year = 1949,
        AuthorId = orwellId
    });

    var fantasyId = await repo.AddGenreAsync(new Genre { Name = "Fantasy" });
    var classicId = await repo.AddGenreAsync(new Genre { Name = "Classic" });
    var dystopiaId = await repo.AddGenreAsync(new Genre { Name = "Dystopia" });

    await repo.AddGenreToBookAsync(hobbitId, fantasyId);
    await repo.AddGenreToBookAsync(fellowshipId, fantasyId);
    await repo.AddGenreToBookAsync(fellowshipId, classicId);
    await repo.AddGenreToBookAsync(book1984Id, dystopiaId);
    await repo.AddGenreToBookAsync(book1984Id, classicId);

    Console.WriteLine("--- Books with genres (QueryMultiple) ---");
    foreach (var book in await repo.GetBooksWithGenresAsync())
    {
        var genres = book.Genres.Count == 0
            ? "(no genres)"
            : string.Join(", ", book.Genres.Select(genre => genre.Name));
        Console.WriteLine($"{book.Title} [{book.Year}] -> {genres}");
    }

    Console.WriteLine("\n--- Search: term=1984 (ILIKE @term) ---");
    foreach (var book in await repo.SearchBooksAsync("1984"))
        Console.WriteLine(book.Title);

    Console.WriteLine("\n--- Page 1, size 2 (LIMIT/OFFSET) ---");
    foreach (var book in await repo.GetBooksPagedAsync(page: 1, pageSize: 2))
        Console.WriteLine(book.Title);

    Console.WriteLine("\n--- Transaction: transfer The Hobbit to George Orwell ---");
    Console.WriteLine(await repo.TransferBookAsync(hobbitId, orwellId)
        ? "Transfer committed."
        : "Transfer rolled back: book was not found.");

    Console.WriteLine("\n--- PostgreSQL function: get_books_by_author ---");
    foreach (var book in await repo.GetBooksByAuthorFunctionAsync(orwellId))
        Console.WriteLine(book.Title);

    const string injectionPayload = "%' OR 1=1 --";
    Console.WriteLine("\n--- SQL injection lesson ---");
    Console.WriteLine("UNSAFE concatenation changes the SQL command structure:");
    try
    {
        var unsafeBooks = await repo.GetBooksUnsafeAsync(injectionPayload);
        Console.WriteLine($"UNSAFE result count: {unsafeBooks.Count()}");
    }
    catch (Exception exception)
    {
        Console.WriteLine($"UNSAFE query failed as expected for this payload: {exception.Message}");
    }

    var safeBooks = await repo.SearchBooksAsync(injectionPayload);
    Console.WriteLine("SAFE ILIKE @term treats the same value as data.");
    Console.WriteLine($"SAFE result count: {safeBooks.Count()}");
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Database demo failed: {exception.Message}");
    return 1;
}

return 0;
