using Microsoft.AspNetCore.Identity;

namespace ArcanumBudget.Api.Models;

// Extends ASP.NET Identity's built-in user with our own fields.
// Identity gives us password hashing, login, email confirmation, etc. for free.
public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PlaidItem> PlaidItems { get; set; } = new List<PlaidItem>();
    public ICollection<HouseholdMember> HouseholdMemberships { get; set; } = new List<HouseholdMember>();
}

public class Household
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<HouseholdMember> Members { get; set; } = new List<HouseholdMember>();
}

public enum HouseholdMemberStatus
{
    Pending,   // invite sent, not yet verified
    Verified,  // accepted, data is shared
    Declined
}

// Join table: links users to households, with an invite/verification workflow.
public class HouseholdMember
{
    public int Id { get; set; }

    public int HouseholdId { get; set; }
    public Household Household { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    public HouseholdMemberStatus Status { get; set; } = HouseholdMemberStatus.Pending;

    // Set when someone sends an invite by email before the invitee even has an account,
    // or to track who invited whom.
    public string? InvitedByUserId { get; set; }
    public AppUser? InvitedByUser { get; set; }
    public string? VerificationToken { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// One PlaidItem = one bank login connection (e.g. "Grant's Bank of America login").
// A single Item can expose multiple accounts (checking, savings, credit card).
public class PlaidItem
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    public string PlaidItemId { get; set; } = string.Empty;

    // NEVER store this in plaintext. Encrypt via IDataProtector before saving.
    public string AccessTokenEncrypted { get; set; } = string.Empty;

    public string InstitutionName { get; set; } = string.Empty;
    public string? Cursor { get; set; } // for /transactions/sync pagination
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncedAt { get; set; }

    public ICollection<Account> Accounts { get; set; } = new List<Account>();
}

public class Account
{
    public int Id { get; set; }

    public int PlaidItemId { get; set; }
    public PlaidItem PlaidItem { get; set; } = null!;

    public string PlaidAccountId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;    // depository, credit, etc.
    public string? Subtype { get; set; }                // checking, savings, credit card

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

public class Transaction
{
    public int Id { get; set; }

    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;

    public string PlaidTransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? PrimaryCategory { get; set; }   // e.g. "FOOD_AND_DRINK"
    public string? DetailedCategory { get; set; }  // e.g. "COFFEE_SHOP"
    public string? MerchantName { get; set; }
    public DateOnly Date { get; set; }
    public bool Pending { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum RecommendationType
{
    RecurringMerchant,
    ForgottenSubscription,
    CategoryTrend
}

public enum RecommendationStatus
{
    Active,
    Dismissed,
    Actioned
}

public class Recommendation
{
    public int Id { get; set; }

    // Scoped to either an individual user or a household — one of these is set.
    public string? UserId { get; set; }
    public int? HouseholdId { get; set; }

    public RecommendationType Type { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public decimal? EstimatedMonthlySavings { get; set; }
    public RecommendationStatus Status { get; set; } = RecommendationStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
