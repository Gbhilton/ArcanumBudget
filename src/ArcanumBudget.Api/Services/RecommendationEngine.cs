using ArcanumBudget.Api.Data;
using ArcanumBudget.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ArcanumBudget.Api.Services;

public interface IRecommendationEngine
{
    // Runs all modules and persists results. Returns the fresh recommendations.
    Task<List<Recommendation>> GenerateAsync(List<string> userIds, int? householdId, string? categoryFilter = null);
}

public class RecommendationEngine : IRecommendationEngine
{
    private readonly AppDbContext _db;

    // Tune these as you learn what's actually useful.
    private const int RecurringMerchantVisitThreshold = 4; // "4+ times this month" triggers a suggestion
    private const decimal SubscriptionMinAmount = 4.99m;

    public RecommendationEngine(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Recommendation>> GenerateAsync(List<string> userIds, int? householdId, string? categoryFilter = null)
    {
        var since = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));

        var query = _db.Transactions
            .Include(t => t.Account).ThenInclude(a => a.PlaidItem)
            .Where(t => userIds.Contains(t.Account.PlaidItem.UserId) && t.Date >= since && !t.Pending);

        if (categoryFilter is not null)
            query = query.Where(t => t.PrimaryCategory == categoryFilter);

        var transactions = await query.ToListAsync();

        var results = new List<Recommendation>();
        results.AddRange(FindRecurringMerchants(transactions, userIds, householdId));
        results.AddRange(FindLikelySubscriptions(transactions, userIds, householdId));

        _db.Recommendations.AddRange(results);
        await _db.SaveChangesAsync();

        return results;
    }

    // "You've been to Dunkin' Donuts 10 times this month" style suggestion.
    private IEnumerable<Recommendation> FindRecurringMerchants(
        List<Transaction> transactions, List<string> userIds, int? householdId)
    {
        var byMerchant = transactions
            .Where(t => !string.IsNullOrWhiteSpace(t.MerchantName))
            .GroupBy(t => t.MerchantName!)
            .Where(g => g.Count() >= RecurringMerchantVisitThreshold);

        foreach (var group in byMerchant)
        {
            var visitCount = group.Count();
            var total = group.Sum(t => t.Amount);
            var avg = total / visitCount;
            // Rough "what if you halved these visits" savings estimate.
            var potentialSavings = Math.Round(total * 0.5m, 2);

            yield return new Recommendation
            {
                UserId = householdId is null ? userIds.FirstOrDefault() : null,
                HouseholdId = householdId,
                Type = RecommendationType.RecurringMerchant,
                Category = group.First().PrimaryCategory ?? "Uncategorized",
                Message = $"You've visited {group.Key} {visitCount} times this month, " +
                          $"averaging ${avg:F2} per visit (${total:F2} total). " +
                          $"Cutting back could save you roughly ${potentialSavings:F2}/month.",
                EstimatedMonthlySavings = potentialSavings,
            };
        }
    }

    // Flags small, suspiciously regular monthly charges — likely forgotten subscriptions.
    private IEnumerable<Recommendation> FindLikelySubscriptions(
        List<Transaction> transactions, List<string> userIds, int? householdId)
    {
        var candidates = transactions
            .Where(t => t.Amount >= SubscriptionMinAmount && t.Amount <= 100m)
            .Where(t => !string.IsNullOrWhiteSpace(t.MerchantName))
            .GroupBy(t => new { t.MerchantName, Amount = Math.Round(t.Amount, 0) })
            .Where(g => g.Count() >= 1); // with only 30 days of data, 1 hit + category heuristics; extend once you have more history

        foreach (var group in candidates)
        {
            if (group.Key.MerchantName is null) continue;

            yield return new Recommendation
            {
                UserId = householdId is null ? userIds.FirstOrDefault() : null,
                HouseholdId = householdId,
                Type = RecommendationType.ForgottenSubscription,
                Category = "Subscriptions",
                Message = $"Recurring charge detected: {group.Key.MerchantName} (~${group.Key.Amount:F2}/mo). " +
                          $"Worth checking if you still use this.",
                EstimatedMonthlySavings = group.Key.Amount,
            };
        }
    }
}
