using ArcanumBudget.Api.Data;
using ArcanumBudget.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ArcanumBudget.Api.Services;

public interface IHouseholdService
{
    Task<HouseholdMember> InviteAsync(string invitingUserId, string inviteeEmail);
    Task<bool> VerifyAsync(string token, string verifyingUserId);
    Task<List<string>> GetHouseholdUserIdsAsync(string userId);
}

public class HouseholdService : IHouseholdService
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;

    public HouseholdService(AppDbContext db, IEmailService email)
    {
        _db = db;
        _email = email;
    }

    // Grant invites his wife: creates (or reuses) a household, adds him as verified,
    // adds her as pending, and emails her a verification link.
    public async Task<HouseholdMember> InviteAsync(string invitingUserId, string inviteeEmail)
    {
        var invitee = await _db.Users.FirstOrDefaultAsync(u => u.Email == inviteeEmail)
            ?? throw new InvalidOperationException("No account found with that email. They need to sign up first.");

        if (invitee.Id == invitingUserId)
            throw new InvalidOperationException("Cannot invite yourself.");

        // Does the inviter already belong to a household? Reuse it; else create one.
        var existingMembership = await _db.HouseholdMembers
            .FirstOrDefaultAsync(m => m.UserId == invitingUserId && m.Status == HouseholdMemberStatus.Verified);

        Household household;
        if (existingMembership is not null)
        {
            household = await _db.Households.FirstAsync(h => h.Id == existingMembership.HouseholdId);
        }
        else
        {
            household = new Household { Name = "My Household" };
            _db.Households.Add(household);
            await _db.SaveChangesAsync();

            _db.HouseholdMembers.Add(new HouseholdMember
            {
                HouseholdId = household.Id,
                UserId = invitingUserId,
                Status = HouseholdMemberStatus.Verified,
                VerifiedAt = DateTime.UtcNow,
            });
        }

        // Don't double-invite.
        var alreadyMember = await _db.HouseholdMembers
            .AnyAsync(m => m.HouseholdId == household.Id && m.UserId == invitee.Id);
        if (alreadyMember)
            throw new InvalidOperationException("That person is already linked to this household.");

        var member = new HouseholdMember
        {
            HouseholdId = household.Id,
            UserId = invitee.Id,
            Status = HouseholdMemberStatus.Pending,
            InvitedByUserId = invitingUserId,
            VerificationToken = Guid.NewGuid().ToString("N"),
        };
        _db.HouseholdMembers.Add(member);
        await _db.SaveChangesAsync();

        await _email.SendHouseholdInviteAsync(invitee.Email!, member.VerificationToken!);

        return member;
    }

    // Invitee clicks the emailed link; we confirm it's really their account confirming, then mark verified.
    public async Task<bool> VerifyAsync(string token, string verifyingUserId)
    {
        var member = await _db.HouseholdMembers
            .FirstOrDefaultAsync(m => m.VerificationToken == token && m.Status == HouseholdMemberStatus.Pending);

        if (member is null || member.UserId != verifyingUserId)
            return false; // wrong token, or someone other than the invitee trying to confirm

        member.Status = HouseholdMemberStatus.Verified;
        member.VerifiedAt = DateTime.UtcNow;
        member.VerificationToken = null;

        await _db.SaveChangesAsync();
        return true;
    }

    // The core query from our earlier design: if the user is in a verified household,
    // return every verified member's user id; otherwise just their own.
    public async Task<List<string>> GetHouseholdUserIdsAsync(string userId)
    {
        var membership = await _db.HouseholdMembers
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Status == HouseholdMemberStatus.Verified);

        if (membership is null)
            return new List<string> { userId };

        return await _db.HouseholdMembers
            .Where(m => m.HouseholdId == membership.HouseholdId && m.Status == HouseholdMemberStatus.Verified)
            .Select(m => m.UserId)
            .ToListAsync();
    }
}

// Minimal interface so HouseholdService doesn't care how email actually gets sent.
// Swap in SendGrid/SMTP/etc. later — free options exist for low volume (e.g. SMTP via Gmail for dev).
public interface IEmailService
{
    Task SendHouseholdInviteAsync(string toEmail, string verificationToken);
}
