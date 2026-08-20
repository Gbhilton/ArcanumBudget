using ArcanumBudget.Api.Data;
using ArcanumBudget.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ArcanumBudget.Api.Services;

public interface ISyncService
{
    Task SyncItemAsync(int plaidItemId);
    Task SyncAllForUserAsync(string userId);
}

public class SyncService : ISyncService
{
    private readonly AppDbContext _db;
    private readonly IPlaidService _plaid;

    public SyncService(AppDbContext db, IPlaidService plaid)
    {
        _db = db;
        _plaid = plaid;
    }

    public async Task SyncAllForUserAsync(string userId)
    {
        var itemIds = await _db.PlaidItems
            .Where(p => p.UserId == userId)
            .Select(p => p.Id)
            .ToListAsync();

        foreach (var id in itemIds)
            await SyncItemAsync(id);
    }

    public async Task SyncItemAsync(int plaidItemId)
    {
        var item = await _db.PlaidItems
            .Include(p => p.Accounts)
            .FirstOrDefaultAsync(p => p.Id == plaidItemId)
            ?? throw new InvalidOperationException($"PlaidItem {plaidItemId} not found");

        var response = await _plaid.SyncTransactionsAsync(item.AccessTokenEncrypted, item.Cursor);

        // Make sure every account Plaid knows about exists locally.
        foreach (var plaidAccount in response.Accounts)
        {
            var existing = item.Accounts.FirstOrDefault(a => a.PlaidAccountId == plaidAccount.AccountId);
            if (existing is null)
            {
                _db.Accounts.Add(new Account
                {
                    PlaidItemId = item.Id,
                    PlaidAccountId = plaidAccount.AccountId,
                    Name = plaidAccount.Name ?? "Account",
                    Type = plaidAccount.Type.ToString() ?? "unknown",
                    Subtype = plaidAccount.Subtype?.ToString(),
                });
            }
        }
        await _db.SaveChangesAsync();

        // Reload accounts now that new ones may have been inserted.
        var accountsByPlaidId = await _db.Accounts
            .Where(a => a.PlaidItemId == item.Id)
            .ToDictionaryAsync(a => a.PlaidAccountId);

        // Added / modified transactions
        foreach (var t in response.Added.Concat(response.Modified))
        {
            if (t.TransactionId is null || t.AccountId is null)
                continue;

            if (!accountsByPlaidId.TryGetValue(t.AccountId, out var account))
                continue;

            var existing = await _db.Transactions
                .FirstOrDefaultAsync(x => x.PlaidTransactionId == t.TransactionId);

            if (existing is null)
            {
                _db.Transactions.Add(new Transaction
                {
                    AccountId = account.Id,
                    PlaidTransactionId = t.TransactionId,
                    Amount = t.Amount ?? 0,
                    PrimaryCategory = t.PersonalFinanceCategory?.Primary,
                    DetailedCategory = t.PersonalFinanceCategory?.Detailed,
#pragma warning disable CS0612 // Name is deprecated by Plaid in favor of MerchantName, but still populated as a fallback
                    MerchantName = t.MerchantName ?? t.Name,
#pragma warning restore CS0612
                    Date = t.Date ?? default,
                    Pending = t.Pending ?? false,
                });
            }
            else
            {
                existing.Amount = t.Amount ?? existing.Amount;
                existing.Pending = t.Pending ?? existing.Pending;
                existing.PrimaryCategory = t.PersonalFinanceCategory?.Primary ?? existing.PrimaryCategory;
                existing.DetailedCategory = t.PersonalFinanceCategory?.Detailed ?? existing.DetailedCategory;
            }
        }

        // Removed transactions
        var removedIds = response.Removed.Select(r => r.TransactionId).ToList();
        if (removedIds.Count > 0)
        {
            var toRemove = await _db.Transactions
                .Where(t => removedIds.Contains(t.PlaidTransactionId))
                .ToListAsync();
            _db.Transactions.RemoveRange(toRemove);
        }

        item.Cursor = response.NextCursor;
        item.LastSyncedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }
}
