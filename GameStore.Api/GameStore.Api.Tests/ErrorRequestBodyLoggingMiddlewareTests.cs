using System.Text;
using GameStore.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace GameStore.Api.Tests;

public sealed class ErrorRequestBodyLoggingMiddlewareTests
{
    [Fact]
    public async Task CapturesRequestAndResponseBodiesForFailedJsonResponse()
    {
        const string requestBody = "{\"input\":\"bad\"}";
        const string responseBody = "{\"error\":\"invalid\"}";
        var context = CreateContext(requestBody);
        context.Response.Body = new MemoryStream();
        var middleware = new ErrorRequestBodyLoggingMiddleware(async httpContext =>
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsync(responseBody);
        });

        await middleware.InvokeAsync(context);

        Assert.Equal(requestBody, context.Items[ErrorRequestBodyLoggingMiddleware.RequestBodyItemKey]);
        Assert.Equal(responseBody, context.Items[ErrorRequestBodyLoggingMiddleware.ResponseBodyItemKey]);
        Assert.Equal(responseBody, await ReadBodyAsync(context.Response.Body));
    }

    [Fact]
    public async Task DoesNotCaptureSuccessfulResponseBody()
    {
        var context = CreateContext("{\"input\":\"ok\"}");
        context.Response.Body = new MemoryStream();
        var middleware = new ErrorRequestBodyLoggingMiddleware(async httpContext =>
        {
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync("{\"result\":\"ok\"}");
        });

        await middleware.InvokeAsync(context);

        Assert.False(context.Items.ContainsKey(ErrorRequestBodyLoggingMiddleware.ResponseBodyItemKey));
    }

    [Fact]
    public async Task TruncatesCapturedResponseAfterFourKilobytes()
    {
        var responseBody = new string('x', 5000);
        var context = CreateContext("{}");
        context.Response.Body = new MemoryStream();
        var middleware = new ErrorRequestBodyLoggingMiddleware(async httpContext =>
        {
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(responseBody);
        });

        await middleware.InvokeAsync(context);

        var captured = Assert.IsType<string>(context.Items[ErrorRequestBodyLoggingMiddleware.ResponseBodyItemKey]);
        Assert.Equal(4096 + " [truncated]".Length, captured.Length);
        Assert.EndsWith(" [truncated]", captured);
        Assert.Equal(responseBody, await ReadBodyAsync(context.Response.Body));
    }

    [Fact]
    public async Task OmitsRequestBodyLargerThanFourKilobytes()
    {
        var context = CreateContext(new string('x', 5000));
        context.Response.Body = new MemoryStream();
        var middleware = new ErrorRequestBodyLoggingMiddleware(httpContext =>
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            httpContext.Response.ContentType = "application/problem+json";
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.False(context.Items.ContainsKey(ErrorRequestBodyLoggingMiddleware.RequestBodyItemKey));
    }

    private static DefaultHttpContext CreateContext(string requestBody)
    {
        var context = new DefaultHttpContext();
        var bytes = Encoding.UTF8.GetBytes(requestBody);
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;
        context.Request.ContentType = "application/json";
        return context;
    }

    private static async Task<string> ReadBodyAsync(Stream body)
    {
        body.Position = 0;
        using var reader = new StreamReader(body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}