using System.Security.Claims;
using ArcanumBudget.Api.Data;
using ArcanumBudget.Api.Models;
using ArcanumBudget.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArcanumBudget.Api.Controllers;

[ApiController]
[Route("api/household")]
[Authorize]
public class HouseholdController : ControllerBase
{
    private readonly IHouseholdService _household;
    private readonly AppDbContext _db;

    public HouseholdController(IHouseholdService household, AppDbContext db)
    {
        _household = household;
        _db = db;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    public record InviteRequest(string Email);

    // "Request household linking" — sends the invitee a verification email.
    [HttpPost("invite")]
    public async Task<IActionResult> Invite([FromBody] InviteRequest request)
    {
        try
        {
            var member = await _household.InviteAsync(CurrentUserId, request.Email);
            return Ok(new { memberId = member.Id, status = member.Status.ToString() });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    public record VerifyRequest(string Token);

    // Invitee confirms the link from the emailed token.
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyRequest request)
    {
        var success = await _household.VerifyAsync(request.Token, CurrentUserId);
        if (!success)
            return BadRequest(new { error = "Invalid or expired verification token." });

        return Ok(new { verified = true });
    }

    public record MemberSummary(string UserId, string DisplayName, string Email, string Status);
    public record HouseholdSummary(bool HasHousehold, List<MemberSummary> Members);

    // Whether the caller belongs to a household, and who's in it — powers the
    // dashboard's household toggle.
    [HttpGet("me")]
    public async Task<IActionResult> GetMyHousehold()
    {
        var userId = CurrentUserId;

        var membership = await _db.HouseholdMembers
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Status == HouseholdMemberStatus.Verified);

        if (membership is null)
            return Ok(new HouseholdSummary(false, new List<MemberSummary>()));

        var members = await _db.HouseholdMembers
            .Include(m => m.User)
            .Where(m => m.HouseholdId == membership.HouseholdId)
            .Select(m => new MemberSummary(m.UserId, m.User.DisplayName, m.User.Email!, m.Status.ToString()))
            .ToListAsync();

        return Ok(new HouseholdSummary(true, members));
    }
}
