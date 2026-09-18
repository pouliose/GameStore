using System.ComponentModel.DataAnnotations;

namespace GameStore.Api.Validation;

public sealed class DataAnnotationsValidationFilter<T> : IEndpointFilter
    where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var model = context.Arguments.OfType<T>().FirstOrDefault();

        if (model is null)
        {
            return await next(context);
        }

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);

        if (Validator.TryValidateObject(model, validationContext, validationResults, validateAllProperties: true))
        {
            return await next(context);
        }

        var errors = validationResults
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty), (result, memberName) => new
            {
                MemberName = memberName,
                ErrorMessage = result.ErrorMessage ?? "The value is invalid."
            })
            .GroupBy(error => error.MemberName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray());

        return Results.ValidationProblem(errors);
    }
}