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
}