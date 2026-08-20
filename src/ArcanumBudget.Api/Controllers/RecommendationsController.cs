using System.Security.Claims;
using ArcanumBudget.Api.Data;
using ArcanumBudget.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArcanumBudget.Api.Controllers;

[ApiController]
[Route("api/recommendations")]
[Authorize]
public class RecommendationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHouseholdService _household;
    private readonly IRecommendationEngine _engine;

    public RecommendationsController(AppDbContext db, IHouseholdService household, IRecommendationEngine engine)
    {
        _db = db;
        _household = household;
        _engine = engine;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<IActionResult> GetActive()
    {
        var userIds = await _household.GetHouseholdUserIdsAsync(CurrentUserId);

        var recs = await _db.Recommendations
            .Where(r => (r.UserId != null && userIds.Contains(r.UserId))
                        || (r.HouseholdId != null))
            .Where(r => r.Status == Models.RecommendationStatus.Active)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(recs);
    }

    public record GenerateRequest(string? Category);

    // The "Generate recommendations for Food" button.
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateRequest request)
    {
        var userIds = await _household.GetHouseholdUserIdsAsync(CurrentUserId);
        var fresh = await _engine.GenerateAsync(userIds, householdId: null, categoryFilter: request.Category);
        return Ok(fresh);
    }

    [HttpPost("{id}/dismiss")]
    public async Task<IActionResult> Dismiss(int id)
    {
        var rec = await _db.Recommendations.FindAsync(id);
        if (rec is null) return NotFound();

        rec.Status = Models.RecommendationStatus.Dismissed;
        await _db.SaveChangesAsync();
        return Ok();
    }
}
