using System.Security.Claims;
using ArcanumBudget.Api.Data;
using ArcanumBudget.Api.Models;
using ArcanumBudget.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArcanumBudget.Api.Controllers;

[ApiController]
[Route("api/plaid")]
[Authorize]
public class PlaidController : ControllerBase
{
    private readonly IPlaidService _plaid;
    private readonly ISyncService _sync;
    private readonly AppDbContext _db;
    private readonly IHouseholdService _household;

    public PlaidController(IPlaidService plaid, ISyncService sync, AppDbContext db, IHouseholdService household)
    {
        _plaid = plaid;
        _sync = sync;
        _db = db;
        _household = household;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    // Frontend calls this first to get a link_token, then opens Plaid Link with it.
    [HttpPost("link-token")]
    public async Task<IActionResult> CreateLinkToken()
    {
        var linkToken = await _plaid.CreateLinkTokenAsync(CurrentUserId);
        return Ok(new { linkToken });
    }

    public record ExchangeRequest(string PublicToken, string InstitutionName);

    // After the user finishes the Plaid Link flow, the frontend sends us the public_token.
    // We exchange it for a permanent access_token and store the connection.
    [HttpPost("exchange")]
    public async Task<IActionResult> ExchangePublicToken([FromBody] ExchangeRequest request)
    {
        var (accessToken, itemId) = await _plaid.ExchangePublicTokenAsync(request.PublicToken);

        var item = new PlaidItem
        {
            UserId = CurrentUserId,
            PlaidItemId = itemId,
            AccessTokenEncrypted = _plaid.Encrypt(accessToken),
            InstitutionName = request.InstitutionName,
        };

        _db.PlaidItems.Add(item);
        await _db.SaveChangesAsync();

        // Kick off an initial sync right away so accounts/transactions show up immediately.
        await _sync.SyncItemAsync(item.Id);

        return Ok(new { itemId = item.Id });
    }

    // Manual "refresh" button on the dashboard.
    [HttpPost("sync")]
    public async Task<IActionResult> SyncAll()
    {
        await _sync.SyncAllForUserAsync(CurrentUserId);
        return Ok();
    }

    public record AccountSummary(
        int AccountId, string Name, string InstitutionName, string Type, string? Subtype,
        string OwnerUserId, string OwnerDisplayName, bool IsMine);

    // Linked accounts for the toggle list on the dashboard, scoped to the caller's
    // household (or just their own accounts if unlinked).
    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts()
    {
        var userId = CurrentUserId;
        var userIds = await _household.GetHouseholdUserIdsAsync(userId);

        var accounts = await _db.Accounts
            .Include(a => a.PlaidItem).ThenInclude(p => p.User)
            .Where(a => userIds.Contains(a.PlaidItem.UserId))
            .OrderBy(a => a.PlaidItem.User.DisplayName).ThenBy(a => a.Name)
            .Select(a => new AccountSummary(
                a.Id,
                a.Name,
                a.PlaidItem.InstitutionName,
                a.Type,
                a.Subtype,
                a.PlaidItem.UserId,
                a.PlaidItem.User.DisplayName,
                a.PlaidItem.UserId == userId))
            .ToListAsync();

        return Ok(accounts);
    }
}
