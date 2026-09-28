namespace GameStore.Api.Endpoints;

internal static class Pagination
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int MaxOffset = 10_000;

    public static string InvalidParametersMessage =>
        $"Page must be positive, pageSize must be between 1 and {MaxPageSize}, and offset must not exceed {MaxOffset}.";

    public static bool IsValid(int page, int pageSize) =>
        page > 0 &&
        pageSize > 0 &&
        pageSize <= MaxPageSize &&
        (long)(page - 1) * pageSize <= MaxOffset;

    public static int GetOffset(int page, int pageSize) =>
        (int)((long)(page - 1) * pageSize);
}