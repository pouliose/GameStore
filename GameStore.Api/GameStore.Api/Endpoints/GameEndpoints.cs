using GameStore.Api.Data;
using GameStore.Api.Dtos;
using GameStore.Api.Validation;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Endpoints;

public static class GameEndpoints
{
    const string GetGameEndpointName = "GetGameById";

    public static void MapGameEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/games");

        group.MapGet("/", async (GameStoreContext dbContext) =>
            await dbContext.Games
                .AsNoTracking()
                .Select(game => new GameDto(
                    game.Id,
                    game.Name,
                    game.Genre!.Name,
                    game.Price,
                    game.ReleaseDate))
                .ToListAsync());

        group.MapGet("/{id}", async (int id, GameStoreContext dbContext) =>
        {
            var game = await dbContext.Games
                .AsNoTracking()
                .Where(game => game.Id == id)
                .Select(game => new GameDto(
                    game.Id,
                    game.Name,
                    game.Genre!.Name,
                    game.Price,
                    game.ReleaseDate))
                .SingleOrDefaultAsync();

            return game is not null ? Results.Ok(game) : Results.NotFound();
        }).WithName(GetGameEndpointName);

        group.MapPost("/", async (CreateGameDto createGameDto, GameStoreContext dbContext) =>
        {
            var genre = await GetOrCreateGenreAsync(dbContext, createGameDto.GenreId);
            var game = new Models.Game
            {
                Name = createGameDto.Name,
                Genre = genre,
                Price = createGameDto.Price,
                ReleaseDate = createGameDto.ReleaseDate
            };

            dbContext.Games.Add(game);
            await dbContext.SaveChangesAsync();

            var response = new GameDtoUIResponse(game.Id, game.Name, genre.Id, game.Price, game.ReleaseDate);
            return Results.CreatedAtRoute(GetGameEndpointName, new { id = response.Id }, response);
        }).AddEndpointFilter<DataAnnotationsValidationFilter<CreateGameDto>>();

        group.MapPut("/{id}", async (int id, UpdateGameDto updateGameDto, GameStoreContext dbContext) =>
        {
            var game = await dbContext.Games.FindAsync(id);
            if (game is null)
            {
                return Results.NotFound();
            }

            var genre = await GetOrCreateGenreAsync(dbContext, updateGameDto.GenreId);
            game.Name = updateGameDto.Name;
            game.Genre = genre;
            game.Price = updateGameDto.Price;
            game.ReleaseDate = updateGameDto.ReleaseDate;
            await dbContext.SaveChangesAsync();

            var updatedGame = new GameDto(game.Id, game.Name, genre.Name, game.Price, game.ReleaseDate);
            return Results.Ok(updatedGame);
        }).AddEndpointFilter<DataAnnotationsValidationFilter<UpdateGameDto>>();

        group.MapDelete("/{id}", async (int id, GameStoreContext dbContext) =>
        {
            var game = await dbContext.Games.FindAsync(id);
            if (game is null)
            {
                return Results.NotFound();
            }

            dbContext.Games.Remove(game);
            await dbContext.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static async Task<Models.Genre> GetOrCreateGenreAsync(
        GameStoreContext dbContext,
        int genreId)
    {
        var genre = await dbContext.Genres
            .FirstOrDefaultAsync(item => item.Id == genreId);

        if (genre is not null)
        {
            return genre;
        }

        genre = new Models.Genre { Id = genreId, Name = string.Empty };
        dbContext.Genres.Add(genre);
        return genre;
    }
}
