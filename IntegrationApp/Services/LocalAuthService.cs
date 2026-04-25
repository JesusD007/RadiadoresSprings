using IntegrationApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IntegrationApp.Services;

/// <summary>
/// Genera tokens JWT localmente usando UsuarioMirror cuando el Core no está disponible.
///
/// INVARIANTE: Este servicio SOLO se usa en modo offline.
/// Cuando Core está activo, la autenticación siempre va al Core (fuente de verdad).
///
/// Los tokens generados aquí son idénticos en estructura y validez a los del Core:
/// — Usan el mismo Jwt:Key, Jwt:Issuer y Jwt:Audience configurados en Integration
///   (que deben coincidir con los del Core via variables de entorno).
/// — Tienen el mismo tiempo de expiración (Jwt:ExpiryMinutes).
/// </summary>
public interface ILocalAuthService
{
    /// <summary>
    /// Valida credenciales contra UsuarioMirror y emite un JWT local.
    /// Devuelve null si el usuario no existe, está inactivo o la contraseña es incorrecta.
    /// </summary>
    Task<LocalAuthResult?> LoginAsync(string username, string password);
}

public record LocalAuthResult(string Token, DateTimeOffset ExpiresAt, string Rol, string NombreCompleto, int? ClienteId);

public class LocalAuthService : ILocalAuthService
{
    private readonly IntegrationDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<LocalAuthService> _logger;

    public LocalAuthService(
        IntegrationDbContext db,
        IConfiguration config,
        ILogger<LocalAuthService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task<LocalAuthResult?> LoginAsync(string username, string password)
    {
        var usuario = await _db.UsuariosMirror
            .FirstOrDefaultAsync(u => u.Username == username && u.EsActivo);

        if (usuario is null)
        {
            _logger.LogWarning("[LocalAuth] Usuario '{Username}' no encontrado en mirror", username);
            return null;
        }

        // Verificar contraseña con BCrypt (el hash fue sincronizado desde Core)
        if (!BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash))
        {
            _logger.LogWarning("[LocalAuth] Contraseña incorrecta para '{Username}'", username);
            return null;
        }

        // Buscar el ClienteId por email en el mirror de clientes.
        // No se almacena en UsuarioMirror para evitar migraciones de esquema;
        // ClientesMirror ya tiene el Email sincronizado desde Core.
        int? clienteId = null;
        if (usuario.Rol.Equals("Cliente", StringComparison.OrdinalIgnoreCase) && usuario.Email is not null)
        {
            var clienteMirror = await _db.ClientesMirror
                .FirstOrDefaultAsync(c => c.Email == usuario.Email && c.EsActivo);
            clienteId = clienteMirror?.CoreId;
        }

        var token = GenerarToken(usuario, clienteId);
        _logger.LogInformation("[LocalAuth] Login offline exitoso para '{Username}' (modo sin Core)", username);
        return token;
    }

    private LocalAuthResult GenerarToken(Domain.Entities.UsuarioMirror usuario, int? clienteId)
    {
        var key = _config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key no configurado");
        var issuer   = _config["Jwt:Issuer"]   ?? "CoreApi";
        var audience = _config["Jwt:Audience"] ?? "IntegrationApp";
        var expiryMinutes = _config.GetValue<int>("Jwt:ExpiryMinutes", 480);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, usuario.Username),
            new Claim(ClaimTypes.Role, usuario.Rol),
            new Claim("nombre", $"{usuario.Nombre} {usuario.Apellido}".Trim()),
            // Marca para identificar tokens emitidos offline (informativo)
            new Claim("auth_mode", "offline"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

        return new LocalAuthResult(
            Token: tokenString,
            ExpiresAt: expiresAt,
            Rol: usuario.Rol,
            NombreCompleto: $"{usuario.Nombre} {usuario.Apellido}".Trim(),
            ClienteId: clienteId);
    }
}
