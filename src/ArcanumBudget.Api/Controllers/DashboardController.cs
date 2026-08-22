using System.Security.Claims;
using ArcanumBudget.Api.Data;
using ArcanumBudget.Api.Models;
using ArcanumBudget.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArcanumBudget.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHouseholdService _household;

    public DashboardController(AppDbContext db, IHouseholdService household)
    {
        _db = db;
        _household = household;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    public record CategorySlice(string Category, decimal Total, int TransactionCount);
    public record MerchantSlice(string Merchant, decimal Total, int TransactionCount);

    // The pie-chart data: spend grouped by category, scoped to the household if one exists.
    // Optional accountIds narrows further to specific linked accounts (the dashboard's
    // per-account toggles) — always intersected with the caller's allowed household
    // user ids, so an account outside the caller's household can never be passed in.
    [HttpGet("spend-by-category")]
    public async Task<IActionResult> SpendByCategory(
        [FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null,
        [FromQuery] int[]? accountIds = null)
    {
        var query = await FilteredTransactionsAsync(from, to, accountIds);

        // Pull the filtered rows and group/aggregate client-side: EF Core 8 can't
        // translate GroupBy + multiple aggregates (Sum and Count together) into SQL
        // when the source involves navigation-property joins like Account.PlaidItem —
        // it throws a translation error regardless of how the query is restructured.
        // A household's transaction volume is small enough that this is fine.
        var rows = await query
            .Select(t => new { Category = t.PrimaryCategory ?? "Uncategorized", t.Amount })
            .ToListAsync();

        var slices = rows
            .GroupBy(t => t.Category)
            .Select(g => new CategorySlice(g.Key, g.Sum(t => t.Amount), g.Count()))
            .OrderByDescending(s => s.Total)
            .ToList();

        return Ok(slices);
    }

    // Drill-down from a category pie slice: same filters as spend-by-category, plus
    // narrowed to one category, grouped by merchant instead.
    [HttpGet("spend-by-merchant")]
    public async Task<IActionResult> SpendByMerchant(
        [FromQuery] string category,
        [FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null,
        [FromQuery] int[]? accountIds = null)
    {
        var query = await FilteredTransactionsAsync(from, to, accountIds);
        query = query.Where(t => (t.PrimaryCategory ?? "Uncategorized") == category);

        var rows = await query
            .Select(t => new { Merchant = t.MerchantName ?? "Unknown Merchant", t.Amount })
            .ToListAsync();

        var slices = rows
            .GroupBy(t => t.Merchant)
            .Select(g => new MerchantSlice(g.Key, g.Sum(t => t.Amount), g.Count()))
            .OrderByDescending(s => s.Total)
            .ToList();

        return Ok(slices);
    }

    private async Task<IQueryable<Transaction>> FilteredTransactionsAsync(
        DateOnly? from, DateOnly? to, int[]? accountIds)
    {
        var userIds = await _household.GetHouseholdUserIdsAsync(CurrentUserId);

        var start = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var query = _db.Transactions
            .Where(t => userIds.Contains(t.Account.PlaidItem.UserId)
                        && t.Date >= start && t.Date <= end
                        && !t.Pending
                        && t.Amount > 0); // positive = money out, in Plaid's convention

        if (accountIds is { Length: > 0 })
            query = query.Where(t => accountIds.Contains(t.AccountId));

        return query;
    }
}
