using Core.API.Authorization;
using Core.API.DTOs.Requests;
using Core.API.DTOs.Responses;
using Core.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Core.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    // ── Endpoints públicos ────────────────────────────────────────────────────

    /// <summary>
    /// Login de usuario o cuenta de servicio.
    /// IntegrationApp usa este endpoint para obtener su JWT con rol Cliente.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest req)
    {
        var result = await authService.LoginAsync(req.Username, req.Password);
        if (result is null)
            return Unauthorized(new ApiResponse<AuthResponse>(false, "Credenciales inválidas.", null));
        return Ok(new ApiResponse<AuthResponse>(true, "Login exitoso.", result));
    }

    /// <summary>Renueva el JWT usando el refresh token (disponible para todos los roles).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh([FromBody] RefreshTokenRequest req)
    {
        var result = await authService.RefreshTokenAsync(req.RefreshToken);
        if (result is null)
            return Unauthorized(new ApiResponse<AuthResponse>(false, "Refresh token inválido o expirado.", null));
        return Ok(new ApiResponse<AuthResponse>(true, "Token renovado.", result));
    }

    /// <summary>Registro web de un nuevo cliente. Crea usuario y cliente atómicamente.</summary>
    [HttpPost("registro")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<UsuarioResponse>>> RegistroWeb([FromBody] RegistroWebRequest req)
    {
        try
        {
            var usuario = await authService.RegistrarClienteWebAsync(req);
            return StatusCode(201, new ApiResponse<UsuarioResponse>(true, "Registro exitoso.", usuario));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiResponse<UsuarioResponse>(false, ex.Message, null));
        }
    }

    // ── Endpoints autenticados — cualquier rol ────────────────────────────────

    /// <summary>Cambiar contraseña propia (disponible para todos los roles autenticados).</summary>
    [HttpPost("cambiar-password")]
    [Authorize(Policy = ApiPolicies.Autenticado)]
    public async Task<ActionResult<ApiResponse<bool>>> CambiarPassword([FromBody] CambiarPasswordRequest req)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ok = await authService.CambiarPasswordAsync(userId, req.PasswordActual, req.PasswordNuevo);
        return ok
            ? Ok(new ApiResponse<bool>(true, "Contraseña actualizada.", true))
            : BadRequest(new ApiResponse<bool>(false, "Contraseña actual incorrecta.", false));
    }

    // ── Endpoints exclusivos del Administrador ────────────────────────────────

    /// <summary>Listar todos los usuarios del sistema. Solo Administrador.</summary>
    [HttpGet("usuarios")]
    [Authorize(Policy = ApiPolicies.AdminSistema)]
    public async Task<ActionResult<ApiResponse<IEnumerable<UsuarioResponse>>>> GetUsuarios()
    {
        var usuarios = await authService.GetUsuariosAsync();
        return Ok(new ApiResponse<IEnumerable<UsuarioResponse>>(true, null, usuarios));
    }

    /// <summary>Crear nuevo usuario. Solo Administrador.</summary>
    [HttpPost("usuarios")]
    [Authorize(Policy = ApiPolicies.AdminSistema)]
    public async Task<ActionResult<ApiResponse<UsuarioResponse>>> CrearUsuario([FromBody] CrearUsuarioRequest req)
    {
        try
        {
            var usuario = await authService.CrearUsuarioAsync(req);
            return Ok(new ApiResponse<UsuarioResponse>(true, "Usuario creado.", usuario));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiResponse<UsuarioResponse>(false, ex.Message, null));
        }
    }
}
