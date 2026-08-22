using ArcanumBudget.Api.Models;
using ArcanumBudget.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ArcanumBudget.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IConfiguration _config;
    private readonly IEmailService _email;

    public AuthController(
        UserManager<AppUser> userManager, SignInManager<AppUser> signInManager,
        IConfiguration config, IEmailService email)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _config = config;
        _email = email;
    }

    public record RegisterRequest(string Email, string Password, string DisplayName);
    public record LoginRequest(string Email, string Password);
    public record AuthResponse(string Token, string UserId, string Email, string DisplayName);

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        var token = GenerateJwt(user);
        return Ok(new AuthResponse(token, user.Id, user.Email!, user.DisplayName));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized(new { error = "Invalid credentials." });

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Unauthorized(new { error = "Invalid credentials." });

        var token = GenerateJwt(user);
        return Ok(new AuthResponse(token, user.Id, user.Email!, user.DisplayName));
    }

    public record ForgotPasswordRequest(string Email);
    public record ResetPasswordRequest(string Email, string Token, string NewPassword);

    // Always returns the same generic response, whether or not that email is
    // registered — otherwise this endpoint would leak which emails have accounts.
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is not null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _email.SendPasswordResetAsync(user.Email!, token);
        }

        return Ok(new { message = "If that email is registered, we've sent a reset link." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return BadRequest(new { errors = new[] { "Invalid or expired reset link." } });

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { reset = true });
    }

    public record UpdateProfileRequest(string DisplayName);
    public record UpdateEmailRequest(string NewEmail, string CurrentPassword);
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

    // Display name only — doesn't touch login credentials, so no password check needed.
    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        user.DisplayName = request.DisplayName;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        var token = GenerateJwt(user);
        return Ok(new AuthResponse(token, user.Id, user.Email!, user.DisplayName));
    }

    // Email doubles as the login username here, so require the current password
    // before changing it — same reasoning as changing a password itself.
    [Authorize]
    [HttpPut("email")]
    public async Task<IActionResult> UpdateEmail([FromBody] UpdateEmailRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var passwordOk = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
        if (!passwordOk)
            return BadRequest(new { errors = new[] { "Current password is incorrect." } });

        var setEmailResult = await _userManager.SetEmailAsync(user, request.NewEmail);
        if (!setEmailResult.Succeeded)
            return BadRequest(new { errors = setEmailResult.Errors.Select(e => e.Description) });

        var setUserNameResult = await _userManager.SetUserNameAsync(user, request.NewEmail);
        if (!setUserNameResult.Succeeded)
            return BadRequest(new { errors = setUserNameResult.Errors.Select(e => e.Description) });

        var token = GenerateJwt(user);
        return Ok(new AuthResponse(token, user.Id, user.Email!, user.DisplayName));
    }

    [Authorize]
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { changed = true });
    }

    private string GenerateJwt(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.DisplayName),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
