using System.ComponentModel.DataAnnotations;

namespace FinancialOS.Api.Validation;

public sealed class ValidationEndpointFilter<TRequest> : IEndpointFilter where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null)
        {
            return await next(context);
        }

        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);
        if (!isValid)
        {
            var errors = validationResults
                .SelectMany(result =>
                {
                    var memberNames = result.MemberNames.Any() ? result.MemberNames : new[] { "request" };
                    return memberNames.Select(member => new KeyValuePair<string, string[]>(member, new[] { result.ErrorMessage ?? "Invalid value" }));
                })
                .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.SelectMany(item => item.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                    StringComparer.OrdinalIgnoreCase);

            return Results.ValidationProblem(errors);
        }

        return await next(context);
    }
}
