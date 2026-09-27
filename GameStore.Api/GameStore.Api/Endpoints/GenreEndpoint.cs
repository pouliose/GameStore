using GameStore.Api.Data;
using GameStore.Api.Dtos.Genre;
using GameStore.Api.Validation;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Endpoints;

public static class GenreEndpoint
{
    private const string GetGenreEndpointName = "GetGenreById";

    public static void MapGenreEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/genres");

        group.MapGet("/", async (GameStoreContext dbContext) =>
            await dbContext.Genres
                .AsNoTracking()
                .Select(genre => new GenreDto(genre.Id, genre.Name))
                .ToListAsync());

        group.MapGet("/{id}", async (int id, GameStoreContext dbContext) =>
        {
            var genre = await dbContext.Genres
                .AsNoTracking()
                .Where(genre => genre.Id == id)
                .Select(genre => new GenreDto(genre.Id, genre.Name))
                .SingleOrDefaultAsync();

            return genre is not null ? Results.Ok(genre) : Results.NotFound();
        }).WithName(GetGenreEndpointName);

        group.MapPost("/", async (CreateGenreDto createGenreDto, GameStoreContext dbContext) =>
        {
            var genre = new Models.Genre { Name = createGenreDto.Name.Trim() };
            dbContext.Genres.Add(genre);
            await dbContext.SaveChangesAsync();

            var response = new GenreDto(genre.Id, genre.Name);
            return Results.CreatedAtRoute(GetGenreEndpointName, new { id = genre.Id }, response);
        }).AddEndpointFilter<DataAnnotationsValidationFilter<CreateGenreDto>>();

        group.MapPut("/{id}", async (int id, UpdateGenreDto updateGenreDto, GameStoreContext dbContext) =>
        {
            var genre = await dbContext.Genres.FindAsync(id);
            if (genre is null)
            {
                return Results.NotFound();
            }

            genre.Name = updateGenreDto.Name.Trim();
            await dbContext.SaveChangesAsync();

            return Results.Ok(new GenreDto(genre.Id, genre.Name));
        }).AddEndpointFilter<DataAnnotationsValidationFilter<UpdateGenreDto>>();

        group.MapDelete("/{id}", async (int id, GameStoreContext dbContext) =>
        {
            var genre = await dbContext.Genres.FindAsync(id);
            if (genre is null)
            {
                return Results.NotFound();
            }

            if (await dbContext.Games.AnyAsync(game => game.GenreId == id))
            {
                return Results.Conflict(new { message = "Cannot delete a genre that is assigned to games." });
            }

            dbContext.Genres.Remove(genre);
            await dbContext.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
