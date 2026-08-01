using System.ComponentModel.DataAnnotations;

namespace FinancialOS.Api.Validation;

public static class KnowledgeRequestValidator
{
    public static Dictionary<string, string[]> Validate(object request)
    {
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);
        if (isValid)
        {
            return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        }

        return validationResults
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
    }
}
