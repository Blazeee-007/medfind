using FluentValidation;
using MedFind.Application.DTOs;
using MedFind.Application.Interfaces;
using MedFind.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace MedFind.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IValidator<AuthRegisterRequest> _registerValidator;
    private readonly IValidator<AuthLoginRequest> _loginValidator;

    public AuthController(IAuthService auth,
        IValidator<AuthRegisterRequest> registerValidator,
        IValidator<AuthLoginRequest> loginValidator)
    {
        _auth = auth;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisteredUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] AuthRegisterRequest request, CancellationToken ct)
    {
        var v = await _registerValidator.ValidateAsync(request, ct);
        if (!v.IsValid)
        {
            foreach (var e in v.Errors) ModelState.AddModelError(e.PropertyName, e.ErrorMessage);
            return ValidationProblem();
        }

        var result = await _auth.RegisterAsync(request, ct);
        return CreatedAtAction(nameof(Register), new { id = result.Id }, result);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] AuthLoginRequest request, CancellationToken ct)
    {
        var v = await _loginValidator.ValidateAsync(request, ct);
        if (!v.IsValid)
        {
            foreach (var e in v.Errors) ModelState.AddModelError(e.PropertyName, e.ErrorMessage);
            return ValidationProblem();
        }

        var result = await _auth.LoginAsync(request, ct);
        return Ok(result);
    }
}
