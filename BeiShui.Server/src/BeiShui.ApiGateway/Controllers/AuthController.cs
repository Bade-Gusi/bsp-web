using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using BeiShui.ApiGateway.Data;
using BeiShui.ApiGateway.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BeiShui.ApiGateway.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    private static readonly Dictionary<string, DateTime> _registerCooldowns = new();
    private static readonly object _cooldownLock = new();
    private static readonly Dictionary<string, (int Count, DateTime LockTime)> _loginFailures = new();
    private const int MaxLoginAttempts = 5;
    private static readonly TimeSpan LoginLockDuration = TimeSpan.FromMinutes(15);

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public record RegisterRequest(string Username, string Password, string Nickname, string? Phone = null, string? SteamId = null, string? MachineFingerprint = null, string? HardwareInfo = null);
    public record LoginRequest(string Username, string Password, string? MachineFingerprint = null, string? HardwareInfo = null);
    public record SteamLoginRequest(string SteamId);
    public record AuthResponse(string Token, UserDto User);
    public record UserDto(long Id, string Username, string Nickname, int Mmr, string RankName, string? Phone = null, string? SteamId = null);

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || req.Username.Length < 3)
            return BadRequest(new { Error = "用户名至少3个字符" });
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
            return BadRequest(new { Error = "密码至少6个字符" });

        // 手机号格式校验
        if (!string.IsNullOrEmpty(req.Phone) && !Regex.IsMatch(req.Phone, @"^1[3-9]\d{9}$"))
            return BadRequest(new { Error = "手机号格式不正确" });

        // SteamId 格式校验
        if (!string.IsNullOrEmpty(req.SteamId) && !Regex.IsMatch(req.SteamId, @"^\d{17}$"))
            return BadRequest(new { Error = "Steam ID 格式不正确" });

        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        lock (_cooldownLock)
        {
            if (_registerCooldowns.TryGetValue(remoteIp, out var lastTime))
            {
                if (DateTime.UtcNow - lastTime < TimeSpan.FromSeconds(60))
                    return BadRequest(new { Error = "注册过于频繁，请稍后再试" });
            }
            _registerCooldowns[remoteIp] = DateTime.UtcNow;
        }

        if (await _db.Users.AnyAsync(u => u.Username == req.Username))
            return Conflict(new { Error = "用户名已存在" });
        if (!string.IsNullOrEmpty(req.Phone) && await _db.Users.AnyAsync(u => u.Phone == req.Phone))
            return Conflict(new { Error = "手机号已被绑定" });
        if (!string.IsNullOrEmpty(req.SteamId) && await _db.Users.AnyAsync(u => u.SteamId == req.SteamId))
            return Conflict(new { Error = "Steam 账号已被绑定" });

        var user = new User
        {
            Username = req.Username,
            PasswordHash = HashPasswordBcrypt(req.Password),
            Nickname = string.IsNullOrWhiteSpace(req.Nickname) ? req.Username : req.Nickname,
            Phone = req.Phone ?? "",
            SteamId = req.SteamId,
            MachineFingerprint = req.MachineFingerprint,
            HardwareInfo = req.HardwareInfo,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "注册成功" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // 暴力破解防护：N次失败后锁定
        lock (_cooldownLock)
        {
            if (_loginFailures.TryGetValue(ip, out var fail))
            {
                if (fail.Count >= MaxLoginAttempts && DateTime.UtcNow - fail.LockTime < LoginLockDuration)
                    return StatusCode(429, new { Error = $"登录失败次数过多，请{LoginLockDuration.TotalMinutes}分钟后再试" });
                if (DateTime.UtcNow - fail.LockTime > LoginLockDuration)
                    _loginFailures.Remove(ip);
            }
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
        if (user == null || !VerifyPasswordBcrypt(req.Password, user.PasswordHash))
        {
            lock (_cooldownLock)
            {
                if (_loginFailures.TryGetValue(ip, out var f))
                    _loginFailures[ip] = (f.Count + 1, DateTime.UtcNow);
                else
                    _loginFailures[ip] = (1, DateTime.UtcNow);
            }
            return Unauthorized(new { Error = "用户名或密码错误" });
        }

        // 登录成功后清除失败记录
        lock (_cooldownLock) { _loginFailures.Remove(ip); }

        user.LastLoginAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(req.MachineFingerprint))
            user.MachineFingerprint = req.MachineFingerprint;
        if (!string.IsNullOrEmpty(req.HardwareInfo))
            user.HardwareInfo = req.HardwareInfo;
        await _db.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        var rank = await _db.Ranks.FindAsync(user.RankId);

        return Ok(new AuthResponse(token, new UserDto(
            user.Id, user.Username, user.Nickname, user.MMR, rank?.Name ?? "未定级", user.Phone, user.SteamId)));
    }

    /// <summary>
    /// Steam 登录：已绑定返回 JWT，未绑定返回需要注册
    /// </summary>
    [HttpPost("steam/login")]
    public async Task<IActionResult> SteamLogin([FromBody] SteamLoginRequest req)
    {
        if (string.IsNullOrEmpty(req.SteamId) || !Regex.IsMatch(req.SteamId, @"^\d{17}$"))
            return BadRequest(new { Error = "Steam ID 格式不正确" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.SteamId == req.SteamId);
        if (user == null)
        {
            return Ok(new { NeedRegister = true, SteamId = req.SteamId });
        }

        user.LastLoginAt = DateTime.UtcNow;
        user.Status = 1;
        await _db.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        var rank = await _db.Ranks.FindAsync(user.RankId);

        return Ok(new AuthResponse(token, new UserDto(
            user.Id, user.Username, user.Nickname, user.MMR, rank?.Name ?? "未定级", user.Phone, user.SteamId)));
    }

    /// <summary>
    /// 已登录用户绑定 Steam 账号
    /// </summary>
    [Authorize]
    [HttpPost("bind-steam")]
    public async Task<IActionResult> BindSteam([FromBody] SteamLoginRequest req)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        if (string.IsNullOrEmpty(req.SteamId) || !Regex.IsMatch(req.SteamId, @"^\d{17}$"))
            return BadRequest(new { Error = "Steam ID 格式不正确" });

        if (await _db.Users.AnyAsync(u => u.SteamId == req.SteamId && u.Id != userId))
            return Conflict(new { Error = "Steam 账号已被其他用户绑定" });

        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.SteamId = req.SteamId;
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Steam 绑定成功" });
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var rank = await _db.Ranks.FindAsync(user.RankId);
        return Ok(new
        {
            user.Id,
            user.Username,
            user.Nickname,
            user.AvatarUrl,
            user.Phone,
            user.SteamId,
            user.Birthday,
            user.MachineFingerprint,
            user.HardwareInfo,
            user.MMR,
            RankName = rank?.Name ?? "未定级",
            user.WinCount,
            user.LoseCount,
            user.TotalGames,
            user.CreatedAt
        });
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(req.Nickname))
            user.Nickname = req.Nickname;
        if (!string.IsNullOrWhiteSpace(req.AvatarUrl))
            user.AvatarUrl = req.AvatarUrl;
        if (!string.IsNullOrEmpty(req.Phone))
        {
            if (!Regex.IsMatch(req.Phone, @"^1[3-9]\d{9}$"))
                return BadRequest(new { Error = "手机号格式不正确" });
            if (await _db.Users.AnyAsync(u => u.Phone == req.Phone && u.Id != userId))
                return Conflict(new { Error = "手机号已被绑定" });
            user.Phone = req.Phone;
        }
        if (!string.IsNullOrEmpty(req.Birthday))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(req.Birthday, @"^\d{2}-\d{2}$"))
                return BadRequest(new { Error = "生日格式不正确（MM-dd）" });
            user.Birthday = req.Birthday;
        }

        await _db.SaveChangesAsync();
        return Ok(new { Message = "更新成功" });
    }

    public record UpdateProfileRequest(string? Nickname, string? AvatarUrl, string? Phone, string? Birthday);

    private long GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? long.Parse(claim.Value) : 0;
    }

    private string GenerateJwtToken(User user)
    {
        var jwtKey = _config["Jwt:Key"] ?? "BeiShuiDefaultJwtKeyForDevelopment_ChangeInProduction!";
        var jwtIssuer = _config["Jwt:Issuer"] ?? "BeiShui";
        var jwtAudience = _config["Jwt:Audience"] ?? "BeiShuiClient";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("nickname", user.Nickname),
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashPasswordBcrypt(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    private static bool VerifyPasswordBcrypt(string password, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(password, hash); }
        catch { return false; }
    }

    // ──────────────── 忘记密码 ────────────────

    static readonly Dictionary<string, (string Code, DateTime Expiry)> _resetCodes = new();

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username))
            return BadRequest(new { Error = "请输入用户名" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
        if (user == null)
            return BadRequest(new { Error = "用户不存在" });

        var code = new Random().Next(100000, 999999).ToString();
        var key = $"{req.Username}_{req.Email}";

        lock (_resetCodes) { _resetCodes[key] = (code, DateTime.UtcNow.AddMinutes(15)); }

        Console.WriteLine($"[忘记密码] 用户={req.Username}, 验证码={code} (开发模式)");
        return Ok(new { Message = "验证码已发送", DevCode = code });
    }

    [HttpPost("verify-code")]
    public IActionResult VerifyCode([FromBody] VerifyCodeRequest req)
    {
        var key = $"{req.Username}_{req.Email ?? ""}";
        lock (_resetCodes)
        {
            if (!_resetCodes.TryGetValue(key, out var entry))
                return BadRequest(new { Error = "验证码未发送或已过期" });
            if (DateTime.UtcNow > entry.Expiry)
            { _resetCodes.Remove(key); return BadRequest(new { Error = "验证码已过期" }); }
            if (entry.Code != req.Code)
                return BadRequest(new { Error = "验证码错误" });
            return Ok(new { Message = "验证成功" });
        }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        var key = $"{req.Username}_{req.Email ?? ""}";
        lock (_resetCodes)
        {
            if (!_resetCodes.TryGetValue(key, out var entry))
                return BadRequest(new { Error = "验证码未发送或已过期" });
            if (DateTime.UtcNow > entry.Expiry)
            { _resetCodes.Remove(key); return BadRequest(new { Error = "验证码已过期" }); }
            if (entry.Code != req.Code)
                return BadRequest(new { Error = "验证码错误" });
            _resetCodes.Remove(key);
        }

        if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 6)
            return BadRequest(new { Error = "密码至少6位" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
        if (user == null) return BadRequest(new { Error = "用户不存在" });

        user.PasswordHash = HashPasswordBcrypt(req.NewPassword);
        await _db.SaveChangesAsync();
        return Ok(new { Message = "密码重置成功" });
    }

    public record ForgotPasswordRequest(string Username, string? Email);
    public record VerifyCodeRequest(string Username, string? Email, string Code);
    public record ResetPasswordRequest(string Username, string? Email, string Code, string NewPassword);
}
