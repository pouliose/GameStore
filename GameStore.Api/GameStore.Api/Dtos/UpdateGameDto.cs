using System.ComponentModel.DataAnnotations;

namespace GameStore.Api.Dtos;

public record UpdateGameDto(
    [property: Required][property: StringLength(50)] string Name,
    [property: Required][property: StringLength(20)] string Genre,
    [property: Range(1,100)]decimal Price,
    DateOnly ReleaseDate
);
