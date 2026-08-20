using Going.Plaid;
using Going.Plaid.Entity;
using Going.Plaid.Link;
using Going.Plaid.Item;
using Going.Plaid.Transactions;
using Microsoft.AspNetCore.DataProtection;

namespace ArcanumBudget.Api.Services;

public interface IPlaidService
{
    Task<string> CreateLinkTokenAsync(string userId);
    Task<(string accessToken, string itemId)> ExchangePublicTokenAsync(string publicToken);
    Task<TransactionsSyncResponse> SyncTransactionsAsync(string accessTokenEncrypted, string? cursor);
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

// Wraps the Going.Plaid client so the rest of the app never touches
// raw access tokens or the SDK directly.
public class PlaidService : IPlaidService
{
    private readonly PlaidClient _client;
    private readonly IDataProtector _protector;

    public PlaidService(PlaidClient client, IDataProtectionProvider dataProtectionProvider)
    {
        _client = client;
        // Purpose string scopes this protector so it can only decrypt what it encrypted.
        _protector = dataProtectionProvider.CreateProtector("PlaidAccessTokens.v1");
    }

    public string Encrypt(string plainText) => _protector.Protect(plainText);
    public string Decrypt(string cipherText) => _protector.Unprotect(cipherText);

    // Step 1: create a Link token so the frontend can open Plaid Link's UI.
    public async Task<string> CreateLinkTokenAsync(string userId)
    {
        var request = new LinkTokenCreateRequest
        {
            User = new LinkTokenCreateRequestUser { ClientUserId = userId },
            ClientName = "Arcanum Budget",
            Products = new List<Products> { Products.Transactions },
            CountryCodes = new List<CountryCode> { CountryCode.Us },
            Language = Language.English,
        };

        var response = await _client.LinkTokenCreateAsync(request);
        if (response.Error is not null)
            throw new InvalidOperationException($"Plaid LinkTokenCreate failed: {response.Error.ErrorMessage}");

        return response.LinkToken;
    }

    // Step 2: after the user finishes Plaid Link in the frontend, it returns a
    // short-lived public_token. Exchange it for a permanent access_token + item_id.
    public async Task<(string accessToken, string itemId)> ExchangePublicTokenAsync(string publicToken)
    {
        var response = await _client.ItemPublicTokenExchangeAsync(
            new ItemPublicTokenExchangeRequest { PublicToken = publicToken });

        if (response.Error is not null)
            throw new InvalidOperationException($"Plaid token exchange failed: {response.Error.ErrorMessage}");

        return (response.AccessToken, response.ItemId);
    }

    // Uses /transactions/sync — cursor-based, so it only pulls new/changed/removed
    // transactions after the first call, instead of re-pulling a date range every time.
    public async Task<TransactionsSyncResponse> SyncTransactionsAsync(string accessTokenEncrypted, string? cursor)
    {
        var accessToken = Decrypt(accessTokenEncrypted);

        var response = await _client.TransactionsSyncAsync(new TransactionsSyncRequest
        {
            AccessToken = accessToken,
            Cursor = cursor,
        });

        if (response.Error is not null)
            throw new InvalidOperationException($"Plaid TransactionsSync failed: {response.Error.ErrorMessage}");

        return response;
    }
}
