using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;
using FinancialOS.Data;
using FinancialOS.Infrastructure.Import;
using FinancialOS.Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IFinancialRepository, InMemoryFinancialRepository>();
builder.Services.AddSingleton<EvidenceImportService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

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

app.Run();
