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

    [Fact]
    public async Task GetGamesReturnsRequestedPageWithMetadata()
    {
        using var factory = new GameStoreApiFactory();
        using var client = factory.CreateClient();
        await factory.InitializeDatabaseAsync();

        var genreResponse = await client.PostAsJsonAsync("/genres", new { name = "Puzzle" });
        var genre = await genreResponse.Content.ReadFromJsonAsync<GenreResponse>();
        Assert.NotNull(genre);

        foreach (var name in new[] { "First Game", "Second Game", "Third Game" })
        {
            var createResponse = await client.PostAsJsonAsync("/games", new
            {
                name,
                genreId = genre.Id,
                price = 10,
                releaseDate = "2024-01-01"
            });
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        }

        var response = await client.GetAsync("/games?page=2&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedGamesResponse>();
        Assert.NotNull(page);
        Assert.Equal(2, page.Page);
        Assert.Equal(1, page.PageSize);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal("Second Game", Assert.Single(page.Items).Name);
    }

    [Fact]
    public async Task GetGamesRejectsInvalidPagination()
    {
        using var factory = new GameStoreApiFactory();
        using var client = factory.CreateClient();
        await factory.InitializeDatabaseAsync();

        var response = await client.GetAsync("/games?page=0&pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var deepPageResponse = await client.GetAsync("/games?page=2147483647&pageSize=100");

        Assert.Equal(HttpStatusCode.BadRequest, deepPageResponse.StatusCode);
    }

    private sealed record GenreResponse(int Id, string Name);
    private sealed record GameResponse(int Id, string Name, string Genre, decimal Price, DateOnly ReleaseDate);
    private sealed record PagedGamesResponse(int Page, int PageSize, int TotalCount, List<GameResponse> Items);
}