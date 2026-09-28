using GameStore.Api.Data;
using GameStore.Api.Endpoints;
using GameStore.Api.Middleware;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
	configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddProblemDetails();
builder.SeedDatabase();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, _, exception) =>
    {
        if (exception is not null || httpContext.Response.StatusCode >= 500)
        {
            return LogEventLevel.Error;
        }

        if (httpContext.Response.StatusCode == StatusCodes.Status404NotFound)
        {
            return LogEventLevel.Information;
        }

        return httpContext.Response.StatusCode >= 400
            ? LogEventLevel.Warning
            : LogEventLevel.Verbose;
    };
    
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        if (httpContext.Response.StatusCode >= 400 &&
            httpContext.Items.TryGetValue(ErrorRequestBodyLoggingMiddleware.RequestBodyItemKey, out var requestBody) &&
            requestBody is not null)
        {
            diagnosticContext.Set("RequestBody", requestBody);
        }

        if (httpContext.Response.StatusCode >= 400 &&
            httpContext.Items.TryGetValue(ErrorRequestBodyLoggingMiddleware.ResponseBodyItemKey, out var responseBody) &&
            responseBody is not null)
        {
            diagnosticContext.Set("ResponseBody", responseBody);
        }
    };
});

app.UseMiddleware<ErrorRequestBodyLoggingMiddleware>();

app.UseExceptionHandler();

app.MapGameEndpoints();
app.MapGenreEndpoints();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.MigrateDatabase();
}

app.UseSwagger();
app.UseSwaggerUI();
app.Run();

public partial class Program;
