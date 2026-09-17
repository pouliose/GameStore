namespace GameStore.Api;

public record class CreateGameDto(
    string Name,
    string Genre,
    decimal Price,
    DateOnly ReleaseDate
)
{

}
