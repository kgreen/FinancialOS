using FinancialOS.Api.Endpoints;
using FinancialOS.Api.Validation;
using FinancialOS.Core.Contracts;
using Microsoft.AspNetCore.Mvc;
using FinancialOS.Core.Knowledge.Deduplication;
using FinancialOS.Core.Knowledge.Normalization;
using FinancialOS.Core.Knowledge.Provenance;
using FinancialOS.Core.Knowledge.Rules;
using FinancialOS.Core.Models;
using FinancialOS.Data;
using FinancialOS.Infrastructure.Import;
using FinancialOS.Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

builder.Services.AddConfiguredDatabase(builder.Configuration);
builder.Services.AddScoped<IFinancialRepository, EfFinancialRepository>();
builder.Services.AddSingleton<EvidenceImportService>();
builder.Services.AddScoped<IRuleEvaluationService, RuleEvaluationService>();
builder.Services.AddScoped<IRuleManagementService, RuleManagementService>();
builder.Services.AddScoped<ProvenanceWriter>();
builder.Services.AddScoped<MerchantAliasService>();
builder.Services.AddScoped<INormalizationPipelineService, NormalizationPipelineService>();
builder.Services.AddScoped<DuplicateScoringService>();
builder.Services.AddScoped<IDuplicateReviewService, DuplicateReviewService>();

var app = builder.Build();

var applyMigrationsOnStartup = app.Environment.IsDevelopment()
    || builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup");
var seedOnStartup = builder.Configuration.GetValue("Database:SeedOnStartup", true);

if (applyMigrationsOnStartup)
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.InitializeAsync(seed: seedOnStartup);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        if (exception is null)
        {
            app.Logger.LogError("Unhandled exception while processing {Path}", context.Request.Path);
        }
        else
        {
            app.Logger.LogError(exception, "Unhandled exception while processing {Path}", context.Request.Path);
        }
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = "The request could not be processed due to an internal error.",
            Instance = context.Request.Path
        });
    });
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapRulesEndpoints();
app.MapNormalizationEndpoints();
app.MapDuplicateEndpoints();

app.MapPost("/api/v1/evidence", async (IFormFile file, EvidenceImportService importService, IFinancialRepository repository, CancellationToken cancellationToken) =>
{
    if (file is null || file.Length == 0)
    {
        return Results.BadRequest("A file is required.");
    }

    await using var stream = file.OpenReadStream();
    var result = await importService.ImportAsync(file.FileName, stream, cancellationToken);
    var evidence = await repository.AddEvidenceAsync(result.Evidence, cancellationToken);

    var record = new FinancialRecord
    {
        EvidenceId = evidence.Id,
        AccountId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Description = $"Imported {evidence.OriginalFileName}",
        Amount = new Money(0m, "USD"),
        OccurredOn = evidence.UploadedAt,
        Status = RecordStatus.Pending,
        ClassificationConfidence = new Confidence(0.2m),
        Provenance = new Provenance("import", "initial")
    };

    await repository.AddRecordAsync(record, cancellationToken);

    return Results.Ok(new EvidenceUploadResponse(
        evidence.Id,
        "accepted",
        evidence.SourceType.ToString().ToLowerInvariant(),
        evidence.OriginalFileName,
        evidence.StoragePath,
        evidence.Sha256Hash,
        result.SizeBytes));
}).DisableAntiforgery();

app.MapGet("/api/v1/evidence/{id:guid}", async (Guid id, IFinancialRepository repository, CancellationToken cancellationToken) =>
{
    var evidence = await repository.GetEvidenceAsync(id, cancellationToken);
    if (evidence is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new EvidenceResponse(
        evidence.Id,
        evidence.SourceType.ToString().ToLowerInvariant(),
        evidence.OriginalFileName,
        evidence.StoragePath,
        evidence.Sha256Hash,
        new FileInfo(evidence.StoragePath).Length,
        evidence.UploadedAt));
});

app.MapGet("/api/v1/records", async (IFinancialRepository repository, CancellationToken cancellationToken) =>
{
    var records = await repository.ListRecordsAsync(cancellationToken);
    var items = records.Select(record => new RecordResponse(
        record.Id,
        record.EvidenceId,
        record.AccountId,
        record.MerchantId,
        record.CategoryId,
        record.Description,
        record.Amount.Amount,
        record.Amount.Currency,
        record.OccurredOn,
        record.Status.ToString(),
        record.ClassificationConfidence?.Score,
        record.Provenance?.RuleName)).ToList();

    return Results.Ok(new RecordListResponse(items, 1, 50));
});

app.MapPost("/api/v1/records/{id:guid}/classify", async (Guid id, RecordClassificationRequest request, IFinancialRepository repository, CancellationToken cancellationToken) =>
{
    var record = await repository.GetRecordAsync(id, cancellationToken);
    if (record is null)
    {
        return Results.NotFound();
    }

    record.CategoryId = request.CategoryId;
    record.MerchantId = request.MerchantId;
    record.Status = RecordStatus.Normalized;
    record.ClassificationConfidence = new Confidence(Math.Clamp(request.Confidence, 0m, 1m));
    record.Provenance = new Provenance("api", request.RuleName ?? "manual");

    var updated = await repository.UpdateRecordAsync(record, cancellationToken);
    if (updated is null)
    {
        return Results.StatusCode(500);
    }

    return Results.Ok(new RecordResponse(
        updated.Id,
        updated.EvidenceId,
        updated.AccountId,
        updated.MerchantId,
        updated.CategoryId,
        updated.Description,
        updated.Amount.Amount,
        updated.Amount.Currency,
        updated.OccurredOn,
        updated.Status.ToString(),
        updated.ClassificationConfidence?.Score,
        updated.Provenance?.RuleName));
}).DisableAntiforgery();

app.MapGet("/api/v1/accounts", async (IFinancialRepository repository, CancellationToken cancellationToken) =>
{
    var accounts = await repository.ListAccountsAsync(cancellationToken);
    return Results.Ok(accounts.Select(item => new ReferenceItemResponse(item.Id, item.Name, "account")));
});

app.MapGet("/api/v1/categories", async (IFinancialRepository repository, CancellationToken cancellationToken) =>
{
    var categories = await repository.ListCategoriesAsync(cancellationToken);
    return Results.Ok(categories.Select(item => new ReferenceItemResponse(item.Id, item.Name, "category")));
});

app.MapGet("/api/v1/merchants", async (IFinancialRepository repository, CancellationToken cancellationToken) =>
{
    var merchants = await repository.ListMerchantsAsync(cancellationToken);
    return Results.Ok(merchants.Select(item => new ReferenceItemResponse(item.Id, item.Name, "merchant")));
});

app.MapGet("/api/v1/rules", async (IFinancialRepository repository, CancellationToken cancellationToken) =>
{
    var rules = await repository.ListRulesAsync(cancellationToken);
    return Results.Ok(rules.Select(item => new ReferenceItemResponse(item.Id, item.Name, "rule")));
});

app.MapPost("/api/v1/planning-scenarios", async (PlanningScenarioCreateRequest request, IFinancialRepository repository, CancellationToken cancellationToken) =>
{
    var scenario = new PlanningScenario
    {
        Name = request.Name,
        Description = request.Description,
        TargetAmount = request.TargetAmount,
        Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency,
        RelatedRecordIds = request.RecordIds?.ToList() ?? new List<Guid>()
    };

    var created = await repository.AddPlanningScenarioAsync(scenario, cancellationToken);
    return Results.Created($"/api/v1/planning-scenarios/{created.Id}", new PlanningScenarioResponse(
        created.Id,
        created.Name,
        created.Description,
        created.TargetAmount,
        created.Currency,
        created.RelatedRecordIds,
        created.CreatedAt));
}).AddEndpointFilter<ValidationEndpointFilter<PlanningScenarioCreateRequest>>();

app.MapGet("/api/v1/planning-scenarios", async (IFinancialRepository repository, CancellationToken cancellationToken) =>
{
    var scenarios = await repository.ListPlanningScenariosAsync(cancellationToken);
    var items = scenarios.Select(scenario => new PlanningScenarioResponse(
        scenario.Id,
        scenario.Name,
        scenario.Description,
        scenario.TargetAmount,
        scenario.Currency,
        scenario.RelatedRecordIds,
        scenario.CreatedAt)).ToList();

    return Results.Ok(new PlanningScenarioListResponse(items, 1, 50));
});

app.MapGet("/api/v1/planning-scenarios/{id:guid}", async (Guid id, IFinancialRepository repository, CancellationToken cancellationToken) =>
{
    var scenario = await repository.GetPlanningScenarioAsync(id, cancellationToken);
    if (scenario is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new PlanningScenarioResponse(
        scenario.Id,
        scenario.Name,
        scenario.Description,
        scenario.TargetAmount,
        scenario.Currency,
        scenario.RelatedRecordIds,
        scenario.CreatedAt));
});

app.Run();

public partial class Program;
