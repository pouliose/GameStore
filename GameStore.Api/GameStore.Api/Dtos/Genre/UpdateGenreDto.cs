using System.ComponentModel.DataAnnotations;

namespace GameStore.Api.Dtos.Genre;

public record UpdateGenreDto(
    [property: Required][property: StringLength(50)] string Name);