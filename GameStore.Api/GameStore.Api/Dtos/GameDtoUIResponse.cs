namespace GameStore.Api.Dtos;

public record GameDtoUIResponse(
    int Id,
    string Name,
    int GenreId,
    decimal Price,
    DateOnly ReleaseDate
);
