using Core.API.DTOs.Requests;
using Core.API.DTOs.Responses;
using Core.Data;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Core.API.Services;

public class AuthService(CoreDbContext db, IConfiguration config, ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResponse?> LoginAsync(string username, string password)
    {
        var usuario = await db.Usuarios.Include(u => u.Sucursal)
            .FirstOrDefaultAsync(u => u.Username == username && u.EsActivo);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash))
        {
            logger.LogWarning("🔒 Login fallido para: {Username}", username);
            return null;
        }

        usuario.UltimoAcceso = DateTime.UtcNow;
        var (token, expiry) = GenerarToken(usuario);
        var refresh = GenerarRefreshToken();
        usuario.RefreshToken = refresh;
        usuario.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await db.SaveChangesAsync();

        logger.LogInformation("✅ Login exitoso: {Username} [{Rol}]", username, usuario.Rol);
        return new AuthResponse(token, refresh, expiry, MapUsuario(usuario));
    }

    public async Task<AuthResponse?> RefreshTokenAsync(string refreshToken)
    {
        var usuario = await db.Usuarios.Include(u => u.Sucursal)
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken
                && u.RefreshTokenExpiry > DateTime.UtcNow && u.EsActivo);
        if (usuario is null) return null;

        var (token, expiry) = GenerarToken(usuario);
        var newRefresh = GenerarRefreshToken();
        usuario.RefreshToken = newRefresh;
        usuario.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await db.SaveChangesAsync();
        return new AuthResponse(token, newRefresh, expiry, MapUsuario(usuario));
    }

    public async Task<bool> CambiarPasswordAsync(int usuarioId, string passwordActual, string passwordNuevo)
    {
        var usuario = await db.Usuarios.FindAsync(usuarioId);
        if (usuario is null || !BCrypt.Net.BCrypt.Verify(passwordActual, usuario.PasswordHash))
            return false;

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordNuevo);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<UsuarioResponse?> CrearUsuarioAsync(CrearUsuarioRequest req)
    {
        if (await db.Usuarios.AnyAsync(u => u.Username == req.Username))
            throw new InvalidOperationException($"El username '{req.Username}' ya existe.");

        if (!Enum.TryParse<RolUsuario>(req.Rol, true, out var rol))
            throw new InvalidOperationException($"Rol '{req.Rol}' no válido.");

        var usuario = new Usuario
        {
            Username = req.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Nombre = req.Nombre,
            Apellido = req.Apellido,
            Email = req.Email,
            Rol = rol,
            SucursalId = req.SucursalId
        };

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        await db.Entry(usuario).Reference(u => u.Sucursal).LoadAsync();
        return MapUsuario(usuario);
    }

    public async Task<IEnumerable<UsuarioResponse>> GetUsuariosAsync()
    {
        return await db.Usuarios.Include(u => u.Sucursal)
            .Where(u => u.EsActivo)
            .Select(u => MapUsuario(u))
            .ToListAsync();
    }

    public async Task<UsuarioResponse> RegistrarClienteWebAsync(RegistroWebRequest req)
    {
        if (await db.Usuarios.AnyAsync(u => u.Username == req.Username))
            throw new InvalidOperationException($"El username '{req.Username}' ya existe.");
        
        if (await db.Usuarios.AnyAsync(u => u.Email == req.Email))
            throw new InvalidOperationException($"El email '{req.Email}' ya existe en Usuarios.");

        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var usuario = new Usuario
            {
                Username = req.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                Nombre = req.Nombre,
                Apellido = req.Apellido,
                Email = req.Email,
                Rol = RolUsuario.Cliente,
                SucursalId = 1 // Sucursal principal por defecto para la web
            };
            db.Usuarios.Add(usuario);

            var cliente = new Cliente
            {
                Nombre = req.Nombre,
                Apellido = req.Apellido,
                Email = req.Email,
                Tipo = TipoCliente.Regular
            };
            db.Clientes.Add(cliente);

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            await db.Entry(usuario).Reference(u => u.Sucursal).LoadAsync();
            return MapUsuario(usuario);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<UsuarioMirrorResponse>> GetUsuariosMirrorAsync()
    {
        var usuarios = await db.Usuarios.ToListAsync();
        return usuarios.Select(u => new UsuarioMirrorResponse(
            u.Id, u.Username, u.PasswordHash, u.Rol.ToString(),
            u.Nombre, u.Apellido, u.Email, u.EsActivo));
    }

    private (string Token, DateTime Expiry) GenerarToken(Usuario usuario)
    {
        var key = config["Jwt:Secret"] ?? "RadiadoresSpringsSecretKey2026!XYZ";
        var issuer = config["Jwt:Issuer"] ?? "CoreApi";
        var audience = config["Jwt:Audience"] ?? "IntegrationApp";
        var expiryMinutes = int.TryParse(config["Jwt:ExpiryMinutes"], out var m) ? m : 480;

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Username),
            new Claim(ClaimTypes.Role, usuario.Rol.ToString()),
            new Claim("sucursal", usuario.SucursalId.ToString()),
            new Claim("nombre", $"{usuario.Nombre} {usuario.Apellido}".Trim())
        };

        var token = new JwtSecurityToken(issuer, audience, claims,
            expires: expiry, signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiry);
    }

    private static string GenerarRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static UsuarioResponse MapUsuario(Usuario u) => new(
        u.Id, u.Username, u.Nombre, u.Apellido, u.Email,
        u.Rol.ToString(), u.SucursalId,
        u.Sucursal?.Nombre ?? "-", u.EsActivo);
}
