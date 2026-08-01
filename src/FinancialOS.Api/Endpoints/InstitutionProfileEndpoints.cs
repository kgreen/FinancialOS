using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;
using FinancialOS.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FinancialOS.Api.Endpoints;

public static class InstitutionProfileEndpoints
{
    public static IEndpointRouteBuilder MapInstitutionProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/institution-profiles");

        group.MapPost("/", CreateProfile);
        group.MapGet("/", ListProfiles);
        group.MapGet("/{id:guid}", GetProfile);
        group.MapPut("/{id:guid}", UpdateProfile);
        group.MapDelete("/{id:guid}", DeleteProfile);

        return app;
    }

    private static async Task<IResult> CreateProfile(
        CreateInstitutionProfileRequest request,
        IFinancialRepository repository,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request.Name, request.ColumnMappings, request.AmountLayout,
            request.DebitColumnName, request.CreditColumnName, request.DateFormatPattern);
        if (validationError is not null)
        {
            return Results.Problem(
                detail: validationError,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.1");
        }

        if (!Enum.TryParse<AmountLayout>(request.AmountLayout, ignoreCase: true, out var amountLayout))
            return Results.Problem(detail: $"Invalid amountLayout '{request.AmountLayout}'",
                statusCode: 400, title: "Bad Request");

        var profile = new InstitutionProfile
        {
            Name = request.Name,
            ColumnMappings = request.ColumnMappings,
            AmountLayout = amountLayout,
            DebitColumnName = request.DebitColumnName,
            CreditColumnName = request.CreditColumnName,
            DateFormatPattern = request.DateFormatPattern,
        };

        try
        {
            var created = await repository.AddInstitutionProfileAsync(profile, cancellationToken);
            return Results.Created(
                $"/api/v1/institution-profiles/{created.Id}",
                MapToResponse(created));
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (
            ex.InnerException?.Message.Contains("UNIQUE") == true ||
            ex.InnerException?.Message.Contains("unique") == true)
        {
            return Results.Problem(
                detail: $"An institution profile named '{request.Name}' already exists.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.10");
        }
    }

    private static async Task<IResult> ListProfiles(
        IFinancialRepository repository,
        CancellationToken cancellationToken)
    {
        var profiles = await repository.ListInstitutionProfilesAsync(cancellationToken);
        return Results.Ok(profiles.Select(MapToResponse).ToList());
    }

    private static async Task<IResult> GetProfile(
        Guid id,
        IFinancialRepository repository,
        CancellationToken cancellationToken)
    {
        var profile = await repository.GetInstitutionProfileAsync(id, cancellationToken);
        if (profile is null)
            return Results.Problem(
                detail: "Institution profile not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");

        return Results.Ok(MapToResponse(profile));
    }

    private static async Task<IResult> UpdateProfile(
        Guid id,
        UpdateInstitutionProfileRequest request,
        IFinancialRepository repository,
        CancellationToken cancellationToken)
    {
        var profile = await repository.GetInstitutionProfileAsync(id, cancellationToken);
        if (profile is null)
            return Results.Problem(detail: "Institution profile not found.", statusCode: 404, title: "Not Found");

        var validationError = ValidateRequest(request.Name, request.ColumnMappings, request.AmountLayout,
            request.DebitColumnName, request.CreditColumnName, request.DateFormatPattern);
        if (validationError is not null)
            return Results.Problem(detail: validationError, statusCode: 400, title: "Bad Request");

        if (!Enum.TryParse<AmountLayout>(request.AmountLayout, ignoreCase: true, out var amountLayout))
            return Results.Problem(detail: $"Invalid amountLayout '{request.AmountLayout}'", statusCode: 400, title: "Bad Request");

        profile.Name = request.Name;
        profile.ColumnMappings = request.ColumnMappings;
        profile.AmountLayout = amountLayout;
        profile.DebitColumnName = request.DebitColumnName;
        profile.CreditColumnName = request.CreditColumnName;
        profile.DateFormatPattern = request.DateFormatPattern;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await repository.UpdateInstitutionProfileAsync(profile, cancellationToken);
        return Results.Ok(MapToResponse(profile));
    }

    private static async Task<IResult> DeleteProfile(
        Guid id,
        IFinancialRepository repository,
        CancellationToken cancellationToken)
    {
        var profile = await repository.GetInstitutionProfileAsync(id, cancellationToken);
        if (profile is null)
            return Results.Problem(detail: "Institution profile not found.", statusCode: 404, title: "Not Found");

        var deleted = await repository.DeleteInstitutionProfileAsync(id, cancellationToken);
        if (!deleted)
        {
            return Results.Problem(
                detail: $"Institution profile '{id}' cannot be deleted because it has been used in import job(s). It has been retained for historical auditability.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.10");
        }

        return Results.NoContent();
    }

    private static string? ValidateRequest(
        string name,
        Dictionary<string, string> columnMappings,
        string amountLayoutStr,
        string? debitColumnName,
        string? creditColumnName,
        string? dateFormatPattern)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Name is required.";
        if (name.Length > 200)
            return "Name must be 200 characters or fewer.";
        if (columnMappings is null || !columnMappings.ContainsKey("date"))
            return "columnMappings must contain the 'date' key.";
        if (!columnMappings.ContainsKey("description"))
            return "columnMappings must contain the 'description' key.";

        if (!Enum.TryParse<AmountLayout>(amountLayoutStr, ignoreCase: true, out var layout))
            return $"Invalid amountLayout '{amountLayoutStr}'. Valid values: singleSigned, splitDebitCredit.";

        if (layout == AmountLayout.SingleSigned && !columnMappings.ContainsKey("amount"))
            return "columnMappings must contain the 'amount' key when amountLayout is 'singleSigned'.";

        if (layout == AmountLayout.SplitDebitCredit)
        {
            if (string.IsNullOrWhiteSpace(debitColumnName))
                return "debitColumnName is required when amountLayout is 'splitDebitCredit'.";
            if (string.IsNullOrWhiteSpace(creditColumnName))
                return "creditColumnName is required when amountLayout is 'splitDebitCredit'.";
        }

        if (dateFormatPattern is not null)
        {
            _ = DateTime.TryParseExact("01/01/2024", dateFormatPattern,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out _);
        }

        return null;
    }

    private static InstitutionProfileResponse MapToResponse(InstitutionProfile p) =>
        new(p.Id, p.Name, p.ColumnMappings,
            p.AmountLayout == AmountLayout.SingleSigned ? "singleSigned" : "splitDebitCredit",
            p.DebitColumnName, p.CreditColumnName, p.DateFormatPattern,
            p.CreatedAt, p.UpdatedAt);
}
