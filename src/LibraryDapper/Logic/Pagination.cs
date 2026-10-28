namespace LibraryDapper.Logic;

public static class Pagination
{
    public static (int Offset, int Size) GetOffset(int page, int pageSize)
    {
        if (page <= 0)
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be positive.");

        if (pageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be positive.");

        return checked(((page - 1) * pageSize, pageSize));
    }
}
