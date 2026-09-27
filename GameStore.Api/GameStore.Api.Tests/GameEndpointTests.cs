using System.Net;
using System.Net.Http.Json;
using GameStore.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameStore.Api.Tests;

public sealed class GameEndpointTests
{
    [Fact]
    public async Task CreateGameRejectsGenreIdThatDoesNotExist()
    {
        using var factory = new GameStoreApiFactory();
        using var client = factory.CreateClient();
        await factory.InitializeDatabaseAsync();

        var response = await client.PostAsJsonAsync("/games", new
        {
            name = "Invalid Genre Game",
            genreId = 50,
            price = 10,
            releaseDate = "2024-01-01"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GameStoreContext>();
        Assert.False(await dbContext.Genres.AnyAsync(genre => genre.Id == 50));
    }

    [Fact]
    public async Task UpdateGameRejectsGenreIdThatDoesNotExist()
    {
        using var factory = new GameStoreApiFactory();
        using var client = factory.CreateClient();
        await factory.InitializeDatabaseAsync();

        var genreResponse = await client.PostAsJsonAsync("/genres", new { name = "Puzzle" });
        var genre = await genreResponse.Content.ReadFromJsonAsync<GenreResponse>();
        Assert.NotNull(genre);

        var createGameResponse = await client.PostAsJsonAsync("/games", new
        {
            name = "Test Game",
            genreId = genre.Id,
            price = 10,
            releaseDate = "2024-01-01"
        });
        var game = await createGameResponse.Content.ReadFromJsonAsync<GameResponse>();
        Assert.NotNull(game);

        var updateResponse = await client.PutAsJsonAsync($"/games/{game.Id}", new
        {
            name = "Updated Game",
            genreId = 50,
            price = 12,
            releaseDate = "2024-01-01"
        });

        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
        var getResponse = await client.GetAsync($"/games/{game.Id}");
        var unchangedGame = await getResponse.Content.ReadFromJsonAsync<GameResponse>();
        Assert.NotNull(unchangedGame);
        Assert.Equal("Test Game", unchangedGame.Name);
        Assert.Equal("Puzzle", unchangedGame.Genre);
    }

    private sealed record GenreResponse(int Id, string Name);
    private sealed record GameResponse(int Id, string Name, string Genre, decimal Price, DateOnly ReleaseDate);
}