using FinancialOS.Core.Models;

namespace FinancialOS.Core.Contracts;

public interface IGoalService
{
    Task<Goal> CreateGoalAsync(Goal goal, CancellationToken cancellationToken = default);
    Task<Goal?> GetGoalAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Goal>> ListGoalsAsync(CancellationToken cancellationToken = default);
    Task<Goal?> UpdateGoalAsync(Goal goal, CancellationToken cancellationToken = default);
    Task<bool> DeleteGoalAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Budget> CreateBudgetAsync(Budget budget, CancellationToken cancellationToken = default);
    Task<Budget?> GetBudgetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Budget>> ListBudgetsAsync(CancellationToken cancellationToken = default);
    Task<Budget?> UpdateBudgetAsync(Budget budget, CancellationToken cancellationToken = default);
    Task<bool> DeleteBudgetAsync(Guid id, CancellationToken cancellationToken = default);
}
