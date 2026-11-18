using MedFind.Application.DTOs;
using MedFind.Application.Interfaces;
using MedFind.Domain.Entities;
using MedFind.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MedFind.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly MedFindDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    // PharmacyCode used to gate PharmacyAdmin registration (env-configurable secret)
    private const string DefaultPharmacyCode = "MEDFIND-ADMIN-2024";

    public AuthService(MedFindDbContext db, IConfiguration config, ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task<RegisteredUserDto> RegisterAsync(AuthRegisterRequest request, CancellationToken ct = default)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email, ct))
            throw new ConflictException($"Email '{request.Email}' is already registered.");

        if (request.Role == "PharmacyAdmin")
        {
            var expectedCode = _config["PharmacyRegistrationCode"] ?? DefaultPharmacyCode;
            if (request.PharmacyCode != expectedCode)
                throw new ForbiddenException("Invalid pharmacy registration code.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Role = request.Role,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("User registered: {Email} as {Role}", user.Email, user.Role);
        return new RegisteredUserDto(user.Id, user.Email, user.Role);
    }

    public async Task<AuthResponse> LoginAsync(AuthLoginRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == request.Email, ct)
            ?? throw new UnauthorizedException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        var pharmacy = user.Role == "PharmacyAdmin"
            ? await _db.Pharmacies.AsNoTracking()
                .FirstOrDefaultAsync(p => p.OwnerUserId == user.Id, ct)
            : null;

        var token = GenerateJwt(user, pharmacy?.Id);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);

        _logger.LogInformation("User logged in: {Email}", user.Email);
        return new AuthResponse(token, expiresAt, user.Role);
    }

    private string GenerateJwt(User user, Guid? pharmacyId)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured")));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
        };

        if (pharmacyId.HasValue)
            claims.Add(new Claim("pharmacyId", pharmacyId.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "medfind",
            audience: _config["Jwt:Audience"] ?? "medfind",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// Domain exceptions — thin, no deps
public class ConflictException(string message) : Exception(message);
public class ForbiddenException(string message) : Exception(message);
public class UnauthorizedException(string message) : Exception(message);
public class NotFoundException(string message) : Exception(message);
