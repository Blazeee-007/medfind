namespace MedFind.Application.DTOs;

public record AuthRegisterRequest(
    string Email,
    string Password,
    string FullName,
    string Role,           // "Patient" | "PharmacyAdmin"
    string? PharmacyCode
);

public record AuthLoginRequest(string Email, string Password);

public record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, string Role);

public record RegisteredUserDto(Guid Id, string Email, string Role);
