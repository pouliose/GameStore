using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace GameStore.Api.Tests;

public sealed class GenreEndpointTests
{
    [Fact]
    public async Task GenreCanBeCreatedReadUpdatedAndDeleted()
    {
        using var factory = new GameStoreApiFactory();
        using var client = factory.CreateClient();
        await factory.InitializeDatabaseAsync();

        var createResponse = await client.PostAsJsonAsync("/genres", new { name = "  Puzzle  " });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<GenreResponse>();
        Assert.NotNull(created);
        Assert.Equal("Puzzle", created.Name);
        Assert.NotNull(createResponse.Headers.Location);

        var getResponse = await client.GetAsync($"/genres/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var updateResponse = await client.PutAsJsonAsync($"/genres/{created.Id}", new { name = "Logic" });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<GenreResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Logic", updated.Name);

        var deleteResponse = await client.DeleteAsync($"/genres/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/genres/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task GetGenresReturnsRequestedPageWithMetadata()
    {
        using var factory = new GameStoreApiFactory();
        using var client = factory.CreateClient();
        await factory.InitializeDatabaseAsync();

        foreach (var name in new[] { "Puzzle", "Action", "Adventure" })
        {
            var createResponse = await client.PostAsJsonAsync("/genres", new { name });
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        }

        var response = await client.GetAsync("/genres?page=2&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedGenresResponse>();
        Assert.NotNull(page);
        Assert.Equal(2, page.Page);
        Assert.Equal(1, page.PageSize);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal("Action", Assert.Single(page.Items).Name);
    }

    [Fact]
    public async Task GetGenresRejectsInvalidPagination()
    {
        using var factory = new GameStoreApiFactory();
        using var client = factory.CreateClient();
        await factory.InitializeDatabaseAsync();

        var response = await client.GetAsync("/genres?page=0&pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GenreGamesEndpointReturnsOnlyGamesInGenre()
    {
        using var factory = new GameStoreApiFactory();
        using var client = factory.CreateClient();
        await factory.InitializeDatabaseAsync();

        var puzzleResponse = await client.PostAsJsonAsync("/genres", new { name = "Puzzle" });
        var puzzle = await puzzleResponse.Content.ReadFromJsonAsync<GenreResponse>();
        Assert.NotNull(puzzle);

        var actionResponse = await client.PostAsJsonAsync("/genres", new { name = "Action" });
        var action = await actionResponse.Content.ReadFromJsonAsync<GenreResponse>();
        Assert.NotNull(action);

        await client.PostAsJsonAsync("/games", new
        {
            name = "Puzzle Game 1",
            genreId = puzzle.Id,
            price = 10,
            releaseDate = "2024-01-01"
        });
        await client.PostAsJsonAsync("/games", new
        {
            name = "Puzzle Game 2",
            genreId = puzzle.Id,
            price = 10,
            releaseDate = "2024-01-01"
        });
        await client.PostAsJsonAsync("/games", new
        {
            name = "Action Game",
            genreId = action.Id,
            price = 20,
            releaseDate = "2024-01-01"
        });

        var response = await client.GetAsync($"/genres/{puzzle.Id}/games?page=2&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedGamesResponse>();
        Assert.NotNull(page);
        Assert.Equal(2, page.Page);
        Assert.Equal(1, page.PageSize);
        Assert.Equal(2, page.TotalCount);
        var game = Assert.Single(page.Items);
        Assert.Equal("Puzzle Game 2", game.Name);
        Assert.Equal("Puzzle", game.Genre);
    }

    [Fact]
    public async Task GenreGamesEndpointReturnsNotFoundForUnknownGenre()
    {
        using var factory = new GameStoreApiFactory();
        using var client = factory.CreateClient();
        await factory.InitializeDatabaseAsync();

        var response = await client.GetAsync("/genres/999/games");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GenreGamesEndpointRejectsInvalidPagination()
    {
        using var factory = new GameStoreApiFactory();
        using var client = factory.CreateClient();
        await factory.InitializeDatabaseAsync();

        var genreResponse = await client.PostAsJsonAsync("/genres", new { name = "Puzzle" });
        var genre = await genreResponse.Content.ReadFromJsonAsync<GenreResponse>();
        Assert.NotNull(genre);

        var response = await client.GetAsync($"/genres/{genre.Id}/games?pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GenreValidationRejectsEmptyName()
    {
        using var factory = new GameStoreApiFactory();
        using var client = factory.CreateClient();
        await factory.InitializeDatabaseAsync();

        var response = await client.PostAsJsonAsync("/genres", new { name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GenreInUseCannotBeDeleted()
    {
        using var factory = new GameStoreApiFactory();
        using var client = factory.CreateClient();
        await factory.InitializeDatabaseAsync();

        var genreResponse = await client.PostAsJsonAsync("/genres", new { name = "Puzzle" });
        var genre = await genreResponse.Content.ReadFromJsonAsync<GenreResponse>();
        Assert.NotNull(genre);

        var gameResponse = await client.PostAsJsonAsync("/games", new
        {
            name = "Test Game",
            genreId = genre.Id,
            price = 10,
            releaseDate = "2024-01-01"
        });
        Assert.Equal(HttpStatusCode.Created, gameResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/genres/{genre.Id}");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
    }

    private sealed record GenreResponse(int Id, string Name);
    private sealed record GameResponse(int Id, string Name, string Genre, decimal Price, DateOnly ReleaseDate);
    private sealed record PagedGamesResponse(int Page, int PageSize, int TotalCount, List<GameResponse> Items);
    private sealed record PagedGenresResponse(int Page, int PageSize, int TotalCount, List<GenreResponse> Items);
}