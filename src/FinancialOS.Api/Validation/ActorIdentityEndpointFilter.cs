namespace FinancialOS.Api.Validation;

public sealed class ActorIdentityEndpointFilter : IEndpointFilter
{
    public const string ActorHeader = "X-Actor-Id";
    public const string ActorContextKey = "ActorId";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        if (!httpContext.Request.Headers.TryGetValue(ActorHeader, out var actorIdHeader) ||
            string.IsNullOrWhiteSpace(actorIdHeader.ToString()))
        {
            return Results.BadRequest(new
            {
                error = "actor-id-required",
                message = $"Header '{ActorHeader}' is required."
            });
        }

        httpContext.Items[ActorContextKey] = actorIdHeader.ToString().Trim();
        return await next(context);
    }
}
