namespace Core.ConsoleUI.Auth;

/// <summary>
/// Matriz de permisos por rol para el menú de la consola.
/// Define qué módulos son visibles y qué operaciones de escritura están permitidas.
/// </summary>
public static class PermisoConsola
{
    // ── Labels exactos del menú principal ────────────────────────────────────
    public const string MProductos = "📦 Productos & Inventario";
    public const string MVentas    = "🧾 Ventas & Facturación";
    public const string MClientes  = "👥 Clientes & Cuentas por Cobrar";
    public const string MCaja      = "🏦 Caja";
    public const string MOrdenes   = "📋 Órdenes";
    public const string MReportes  = "📊 Reportes y Estadísticas";
    public const string MBus       = "🔌 Monitor del Bus de Mensajes";
    public const string MUsuarios  = "⚙️  Usuarios & Sistema";
    public const string MSalir     = "🚪 Salir";

    private static readonly IReadOnlyList<string> TodosLosModulos =
    [
        MProductos, MVentas, MClientes, MCaja,
        MOrdenes, MReportes, MBus, MUsuarios, MSalir
    ];

    // ── Menú top-level por rol ────────────────────────────────────────────────
    /// <summary>
    /// Retorna los módulos del menú principal disponibles para el rol dado.
    /// </summary>
    public static IReadOnlyList<string> MenuPermitido(string rol) => rol switch
    {
        "Administrador" => TodosLosModulos,
        "Cajero"        => [MProductos, MVentas, MClientes, MCaja, MSalir],
        "Vendedor"      => [MProductos, MVentas, MClientes, MOrdenes, MSalir],
        "Almacenista"   => [MProductos, MSalir],
        _               => [MSalir]
    };

    // ── Permisos de escritura granulares ──────────────────────────────────────

    /// <summary>Puede crear, editar productos y ajustar stock.</summary>
    public static bool PuedeEscribirProductos(string rol)
        => rol is "Administrador" or "Almacenista";

    /// <summary>Puede crear clientes y registrar pagos.</summary>
    public static bool PuedeEscribirClientes(string rol)
        => rol is "Administrador" or "Vendedor";

    /// <summary>Puede cancelar ventas (operación destructiva).</summary>
    public static bool PuedeCancelarVentas(string rol)
        => rol is "Administrador";

    /// <summary>Puede gestionar usuarios del sistema.</summary>
    public static bool PuedeGestionarUsuarios(string rol)
        => rol is "Administrador";

    /// <summary>Puede ver reportes y estadísticas.</summary>
    public static bool PuedeVerReportes(string rol)
        => rol is "Administrador";

    /// <summary>Descripción del nivel de acceso para mostrar en la UI.</summary>
    public static string DescripcionAcceso(string rol) => rol switch
    {
        "Administrador" => "Acceso total al sistema",
        "Cajero"        => "Ventas, Caja y consulta de clientes",
        "Vendedor"      => "Ventas, Clientes y Órdenes",
        "Almacenista"   => "Gestión de inventario",
        _               => "Acceso restringido"
    };
}
