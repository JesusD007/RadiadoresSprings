namespace Core.ConsoleUI.Auth;

/// <summary>
/// Contexto de la sesión activa en la consola.
/// Se crea al autenticar y se pasa a todos los menús que requieren identidad.
/// </summary>
public record ConsoleSession(
    int    UsuarioId,
    string Username,
    string NombreCompleto,
    string Rol,
    string NombreSucursal
)
{
    /// <summary>Color Spectre.Console para el badge del rol en el header.</summary>
    public string RolColor => Rol switch
    {
        "Administrador" => "red",
        "Cajero"        => "blue",
        "Vendedor"      => "green",
        "Almacenista"   => "yellow3",
        _               => "grey"
    };

    /// <summary>Markup completo del badge de rol.</summary>
    public string RolBadge =>
        $"[bold {RolColor}]{Spectre.Console.Markup.Escape(Rol)}[/]";
}
