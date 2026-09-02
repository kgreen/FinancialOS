using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;

namespace FinancialOS.Core.Services;

public sealed class GoalService : IGoalService
{
    private readonly IFinancialRepository _repository;

    public GoalService(IFinancialRepository repository)
    {
        _repository = repository;
    }

    public Task<Goal> CreateGoalAsync(Goal goal, CancellationToken cancellationToken = default) =>
        _repository.AddGoalAsync(goal, cancellationToken);

    public Task<Goal?> GetGoalAsync(Guid id, CancellationToken cancellationToken = default) =>
        _repository.GetGoalAsync(id, cancellationToken);

    public Task<IReadOnlyList<Goal>> ListGoalsAsync(CancellationToken cancellationToken = default) =>
        _repository.ListGoalsAsync(cancellationToken);

    public Task<Goal?> UpdateGoalAsync(Goal goal, CancellationToken cancellationToken = default) =>
        _repository.UpdateGoalAsync(goal, cancellationToken);

    public Task<bool> DeleteGoalAsync(Guid id, CancellationToken cancellationToken = default) =>
        _repository.DeleteGoalAsync(id, cancellationToken);

    public Task<Budget> CreateBudgetAsync(Budget budget, CancellationToken cancellationToken = default) =>
        _repository.AddBudgetAsync(budget, cancellationToken);

    public Task<Budget?> GetBudgetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _repository.GetBudgetAsync(id, cancellationToken);

    public Task<IReadOnlyList<Budget>> ListBudgetsAsync(CancellationToken cancellationToken = default) =>
        _repository.ListBudgetsAsync(cancellationToken);

    public Task<Budget?> UpdateBudgetAsync(Budget budget, CancellationToken cancellationToken = default) =>
        _repository.UpdateBudgetAsync(budget, cancellationToken);

    public Task<bool> DeleteBudgetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _repository.DeleteBudgetAsync(id, cancellationToken);
}
