using MedFind.Application.DTOs;

namespace MedFind.Application.Interfaces;

public interface IAuthService
{
    Task<RegisteredUserDto> RegisterAsync(AuthRegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(AuthLoginRequest request, CancellationToken ct = default);
}
