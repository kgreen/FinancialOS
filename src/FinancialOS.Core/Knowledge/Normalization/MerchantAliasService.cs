using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;

namespace FinancialOS.Core.Knowledge.Normalization;

/// <summary>
/// Result of resolving raw merchant text against known aliases.
/// </summary>
public sealed record AliasResolutionResult(
    Guid CanonicalMerchantId,
    Guid? DefaultCategoryId,
    Guid AliasId,
    string AliasRawText,
    string MatchStrategy,
    decimal ConfidenceWeight);

/// <summary>
/// Manages canonical merchants and their alias mappings, and resolves raw merchant text
/// to a canonical merchant identity using deterministic, stable matching.
/// </summary>
public sealed class MerchantAliasService
{
    private readonly IFinancialRepository _repository;

    public MerchantAliasService(IFinancialRepository repository)
    {
        _repository = repository;
    }

    public async Task<CanonicalMerchant> CreateCanonicalMerchantAsync(CanonicalMerchant merchant, CancellationToken cancellationToken = default)
    {
        merchant.NormalizedKey = Normalize(merchant.NormalizedKey);
        return await _repository.AddCanonicalMerchantAsync(merchant, cancellationToken);
    }

    public async Task<MerchantAliasMap> CreateAliasAsync(MerchantAliasMap alias, CancellationToken cancellationToken = default)
    {
        alias.AliasNormalizedText = Normalize(
            string.IsNullOrWhiteSpace(alias.AliasNormalizedText) ? alias.AliasRawText : alias.AliasNormalizedText);
        return await _repository.AddMerchantAliasAsync(alias, cancellationToken);
    }

    public async Task<IReadOnlyList<CanonicalMerchant>> ListCanonicalMerchantsAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.ListCanonicalMerchantsAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MerchantAliasMap>> ListAliasesAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.ListMerchantAliasesAsync(cancellationToken);
    }

    /// <summary>
    /// Resolves raw merchant text to the best-matching canonical merchant, using deterministic
    /// tie-breaking: highest confidence weight, then oldest alias, then lowest id.
    /// </summary>
    public async Task<AliasResolutionResult?> ResolveAsync(string rawMerchantText, CancellationToken cancellationToken = default)
    {
        var normalizedInput = Normalize(rawMerchantText);
        if (string.IsNullOrWhiteSpace(normalizedInput))
        {
            return null;
        }

        var aliases = await _repository.ListMerchantAliasesAsync(cancellationToken);

        var best = aliases
            .Where(alias => alias.IsActive && IsMatch(alias, normalizedInput))
            .OrderByDescending(alias => alias.ConfidenceWeight)
            .ThenBy(alias => alias.CreatedAtUtc)
            .ThenBy(alias => alias.Id)
            .FirstOrDefault();

        if (best is null)
        {
            return null;
        }

        var canonicalMerchant = await _repository.GetCanonicalMerchantAsync(best.CanonicalMerchantId, cancellationToken);

        return new AliasResolutionResult(
            CanonicalMerchantId: best.CanonicalMerchantId,
            DefaultCategoryId: canonicalMerchant?.DefaultCategoryId,
            AliasId: best.Id,
            AliasRawText: best.AliasRawText,
            MatchStrategy: best.MatchStrategy.ToString(),
            ConfidenceWeight: best.ConfidenceWeight);
    }

    public static string Normalize(string text) =>
        string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim().ToLowerInvariant();

    private static bool IsMatch(MerchantAliasMap alias, string normalizedInput)
    {
        var aliasText = alias.AliasNormalizedText;
        if (string.IsNullOrWhiteSpace(aliasText))
        {
            return false;
        }

        return alias.MatchStrategy switch
        {
            AliasMatchStrategy.Exact => normalizedInput == aliasText,
            AliasMatchStrategy.Contains => normalizedInput.Contains(aliasText, StringComparison.Ordinal),
            AliasMatchStrategy.TokenSet => IsTokenSetMatch(normalizedInput, aliasText),
            _ => false
        };
    }

    private static bool IsTokenSetMatch(string normalizedInput, string aliasText)
    {
        var inputTokens = Tokenize(normalizedInput);
        var aliasTokens = Tokenize(aliasText);
        if (aliasTokens.Count == 0)
        {
            return false;
        }

        // Every alias token must be present in the input token set.
        return aliasTokens.All(inputTokens.Contains);
    }

    private static HashSet<string> Tokenize(string text) =>
        text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);
}
