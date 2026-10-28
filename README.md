# Library Dapper Homework

This is a .NET 8 console application for a PostgreSQL/Dapper homework assignment.
It demonstrates:

- `authors` and `books` with the original CRUD operations;
- `genres` plus `book_genres` for a many-to-many books/genres relation;
- `QueryMultipleAsync` for loading books, genres, and link rows in one command;
- title search with parameterized `ILIKE @term`;
- pagination with `LIMIT @size OFFSET @offset`;
- a book-author transfer inside `BeginTransaction()`;
- a PostgreSQL table-valued function called with Dapper;
- an intentionally unsafe SQL-concatenation example next to the safe version.

## Requirements

- .NET 8 SDK or newer;
- PostgreSQL running on localhost;
- a database and user matching the connection string, or a custom connection string in `ConnectionStrings__Library`.

The default connection string is:

```text
Host=localhost;Port=5432;Database=library;Username=lab;Password=lab123
```

## Setup

Create the database and apply the schema:

```bash
createdb -h localhost -U lab library
psql -h localhost -U lab -d library -f src/LibraryDapper/schema.sql
```

Run the demonstration:

```bash
dotnet run --project src/LibraryDapper/LibraryDapper.csproj
```

On PowerShell, a custom connection string can be supplied as:

```powershell
$env:ConnectionStrings__Library = "Host=localhost;Port=5432;Database=library;Username=lab;Password=lab123"
dotnet run --project src/LibraryDapper/LibraryDapper.csproj
```

The console program inserts sample rows on every run. Use a clean database when you want to repeat the exact output.

## SQL injection demonstration

`GetBooksUnsafeAsync` intentionally interpolates the search value into SQL. With a payload such as `%' OR 1=1 --`, the value can close the string literal and add a new SQL predicate.

`SearchBooksAsync` uses:

```sql
WHERE title ILIKE @term
```

Dapper sends the search text as a parameter, so the same payload remains data and cannot change the SQL command. The unsafe method exists only for the lesson and must never be used with user input.

## Tests

Run unit tests for pagination and many-to-many mapping:

```bash
dotnet test
```

Build the solution:

```bash
dotnet build --configuration Release
```
