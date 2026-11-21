using EmergencyDesk.Server.Data;
using EmergencyDesk.Server.Dtos;
using EmergencyDesk.Server.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmergencyDesk.Server.Controllers;

[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokens;
    private readonly IPasswordHasher<object> _hasher;

    public AuthController(AppDbContext db, TokenService tokens, IPasswordHasher<object> hasher)
        => (_db, _tokens, _hasher) = (db, tokens, hasher);

    // ===== Mobile app =====

    [HttpPost("api/auth/register")]
    public async Task<IActionResult> Register(MobileRegisterRequest r)
    {
        var email = r.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(r.FullName) || string.IsNullOrWhiteSpace(email) || r.Password.Length < 6)
            return BadRequest("Name, email and a password of at least 6 characters are required.");
        if (await _db.AppUsers.AnyAsync(u => u.Email == email))
            return Conflict("This email is already registered.");

        var user = new AppUser
        {
            FullName = r.FullName.Trim(),
            Phone = r.Phone.Trim(),
            Email = email,
            PasswordHash = _hasher.HashPassword(new object(), r.Password)
        };
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();
        return Ok(ToAuth(user));
    }

    [HttpPost("api/auth/login")]
    public async Task<IActionResult> Login(MobileLoginRequest r)
    {
        var email = r.Email.Trim().ToLowerInvariant();
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null || _hasher.VerifyHashedPassword(new object(), user.PasswordHash, r.Password)
                            == PasswordVerificationResult.Failed)
            return Unauthorized("Wrong email or password.");
        if (!user.IsActive)
            return StatusCode(403, "This account has been deactivated.");
        return Ok(ToAuth(user));
    }

    private MobileAuthResponse ToAuth(AppUser u) =>
        new(u.Id, u.FullName, u.Phone, u.Email, _tokens.Create(u.Id, u.FullName, "User"));

    // ===== Desktop app =====

    [HttpPost("api/staff/login")]
    public async Task<IActionResult> StaffLogin(StaffLoginRequest r)
    {
        var staff = await _db.Staff.FirstOrDefaultAsync(s => s.Username == r.Username.Trim());
        if (staff is null || _hasher.VerifyHashedPassword(new object(), staff.PasswordHash, r.Password)
                             == PasswordVerificationResult.Failed)
            return Unauthorized();
        return Ok(new StaffLoginResponse(_tokens.Create(staff.Id, staff.DisplayName, staff.Role),
                                         staff.DisplayName, staff.Role));
    }
}
