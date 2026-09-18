using System;
using GameStore.Api.Dtos;
using GameStore.Api.Validation;

namespace GameStore.Api.Endpoints;
public static class GameEndpoints
{
    const string GetGameEndpointName = "GetGameById";

    private static readonly List<GameDto> games =
[
    new(1, "The Legend of Zelda: Breath of the Wild", "Action-adventure", 59.99m, new DateOnly(2017, 3, 3)),
    new (2, "Super Mario Odyssey", "Platformer", 49.99m, new DateOnly(2017, 10, 27)),
    new (3, "Red Dead Redemption 2", "Action-adventure", 39.99m, new DateOnly(2018, 10, 26)),
    new (4, "The Witcher 3: Wild Hunt", "Action RPG", 29.99m, new DateOnly(2015, 5, 19)),
    new (5, "Minecraft", "Sandbox", 26.95m, new DateOnly(2011, 11, 18))
];

    public static void MapGameEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/games");

        group.MapGet("/", () => games);

        group.MapGet("/{id}", (int id) =>
        {
            var game = games.FirstOrDefault(g => g.Id == id);
            return game is not null ? Results.Ok(game) : Results.NotFound();
        }).WithName(GetGameEndpointName);

        group.MapPost("/", (CreateGameDto createGameDto) =>
        {
            var newId = games.Max(g => g.Id) + 1;
            var newGame = new GameDto(newId, createGameDto.Name, createGameDto.Genre, createGameDto.Price, createGameDto.ReleaseDate);
            games.Add(newGame);
            return Results.CreatedAtRoute(GetGameEndpointName, new { id = newId }, newGame);
        }).AddEndpointFilter<DataAnnotationsValidationFilter<CreateGameDto>>();

        group.MapPut("/{id}", (int id, UpdateGameDto updateGameDto) =>
        {
            var game = games.FirstOrDefault(g => g.Id == id);
            if (game is null)
            {
                return Results.NotFound();
            }

            games.Remove(game);
            var updatedGame = new GameDto(id, updateGameDto.Name, updateGameDto.Genre, updateGameDto.Price, updateGameDto.ReleaseDate);
            games.Add(updatedGame);
            return Results.Ok(updatedGame);
        }).AddEndpointFilter<DataAnnotationsValidationFilter<UpdateGameDto>>();

        group.MapDelete("/{id}", (int id) =>
        {
            var game = games.FirstOrDefault(g => g.Id == id);
            if (game is null)
            {
                return Results.NotFound();
            }

            games.Remove(game);
            return Results.NoContent();
        });
    }
}
